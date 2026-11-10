import { z } from "zod";
import type { PendingRequest, ProductHistory, Recommendation } from "./types.js";
import { scoreRequest } from "./recommender.js";

const llmAnswer = z.object({
  decision: z.enum(["approve", "review", "reject"]),
  rationale: z.array(z.string().min(3)).min(1).max(4),
  risks: z.array(z.string().min(3)).max(4),
});

const SYSTEM_PROMPT = [
  "Du bewertest Bestellanforderungen fuer ein Einkaufssystem.",
  "Antworte ausschliesslich mit JSON: {decision, rationale[], risks[]}.",
  "decision ist approve, review oder reject.",
  "Stuetze dich nur auf die gelieferten Zahlen und den Begruendungstext.",
  "Erfinde keine Lieferanten, Preise oder historischen Werte.",
].join(" ");

export interface LlmConfig {
  provider: "gemini" | "openai";
  apiKey: string;
  model: string;
  timeoutMs: number;
}

export function resolveLlmConfig(env: NodeJS.ProcessEnv): LlmConfig | null {
  const timeoutMs = Number(env.LLM_TIMEOUT_MS ?? 8000);
  if (env.GEMINI_API_KEY) {
    return {
      provider: "gemini",
      apiKey: env.GEMINI_API_KEY,
      model: env.GEMINI_MODEL ?? "gemini-2.0-flash",
      timeoutMs,
    };
  }
  if (env.OPENAI_API_KEY) {
    return {
      provider: "openai",
      apiKey: env.OPENAI_API_KEY,
      model: env.OPENAI_MODEL ?? "gpt-4o-mini",
      timeoutMs,
    };
  }
  return null;
}

function buildUserPrompt(
  request: PendingRequest,
  history: ProductHistory | undefined,
  signals: Recommendation["signals"],
): string {
  return JSON.stringify({
    produkt: { name: request.productName, sku: request.sku, einheit: request.unit },
    angeforderteMenge: request.quantity,
    aktuellerBestand: request.currentStock,
    bestandsdeckung: signals.stockCoverage,
    mengenVerhaeltnisZum90TageSchnitt: signals.quantityRatio,
    historischeFaelle: signals.historySamples,
    ablehnungsquote: signals.historyRejectionRate,
    weitereOffeneAnforderungen: signals.duplicatePending,
    begruendung: request.justification,
    anfragender: request.requester,
    historie: history ?? null,
  });
}

async function callProvider(
  config: LlmConfig,
  prompt: string,
  signal: AbortSignal,
): Promise<unknown> {
  if (config.provider === "gemini") {
    const url = `https://generativelanguage.googleapis.com/v1beta/models/${config.model}:generateContent`;
    const response = await fetch(url, {
      method: "POST",
      signal,
      headers: { "content-type": "application/json", "x-goog-api-key": config.apiKey },
      body: JSON.stringify({
        systemInstruction: { parts: [{ text: SYSTEM_PROMPT }] },
        contents: [{ role: "user", parts: [{ text: prompt }] }],
        generationConfig: { responseMimeType: "application/json", temperature: 0.2 },
      }),
    });
    if (!response.ok) throw new Error(`gemini ${response.status}`);
    const body = (await response.json()) as {
      candidates?: { content?: { parts?: { text?: string }[] } }[];
    };
    const text = body.candidates?.[0]?.content?.parts?.[0]?.text;
    return text ? JSON.parse(text) : null;
  }

  const response = await fetch("https://api.openai.com/v1/chat/completions", {
    method: "POST",
    signal,
    headers: { "content-type": "application/json", authorization: `Bearer ${config.apiKey}` },
    body: JSON.stringify({
      model: config.model,
      temperature: 0.2,
      response_format: { type: "json_object" },
      messages: [
        { role: "system", content: SYSTEM_PROMPT },
        { role: "user", content: prompt },
      ],
    }),
  });
  if (!response.ok) throw new Error(`openai ${response.status}`);
  const body = (await response.json()) as {
    choices?: { message?: { content?: string } }[];
  };
  const text = body.choices?.[0]?.message?.content;
  return text ? JSON.parse(text) : null;
}

export async function recommend(
  request: PendingRequest,
  history: ProductHistory | undefined,
  duplicatePending: number,
  config: LlmConfig | null,
): Promise<Recommendation> {
  const base = scoreRequest(request, history, duplicatePending);
  if (!config) return { ...base, source: "rules", model: "rule-engine" };

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), config.timeoutMs);
  try {
    const raw = await callProvider(
      config,
      buildUserPrompt(request, history, base.signals),
      controller.signal,
    );
    const parsed = llmAnswer.safeParse(raw);
    if (!parsed.success) throw new Error("schema mismatch");
    return {
      ...base,
      decision: parsed.data.decision,
      rationale: parsed.data.rationale,
      risks: parsed.data.risks,
      source: "llm",
      model: config.model,
    };
  } catch {
    return { ...base, source: "rules", model: "rule-engine" };
  } finally {
    clearTimeout(timer);
  }
}
