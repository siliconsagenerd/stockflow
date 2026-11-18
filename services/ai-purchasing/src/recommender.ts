import type {
  Decision,
  PendingRequest,
  ProductHistory,
  Recommendation,
  Signals,
} from "./types.js";

const round = (value: number) => Math.round(value * 10) / 10;

export function buildSignals(
  request: PendingRequest,
  history: ProductHistory | undefined,
  duplicatePending: number,
): Signals {
  const quantity = Math.max(request.quantity, 1);
  const sampleSize = history?.sampleSize ?? 0;
  const averageQuantity = history?.averageQuantity ?? 0;
  const rejectedCount = history?.rejectedCount ?? 0;

  return {
    stockCoverage: round(request.currentStock / quantity),
    quantityRatio:
      sampleSize >= 3 && averageQuantity > 0
        ? round(quantity / averageQuantity)
        : null,
    justificationLength: (request.justification ?? "").trim().length,
    duplicatePending,
    historyRejectionRate:
      sampleSize >= 3 ? round(rejectedCount / sampleSize) : null,
    historySamples: sampleSize,
  };
}

export function scoreRequest(
  request: PendingRequest,
  history: ProductHistory | undefined,
  duplicatePending: number,
): Omit<Recommendation, "source" | "model"> {
  const signals = buildSignals(request, history, duplicatePending);
  const rationale: string[] = [];
  const risks: string[] = [];
  let score = 60;

  if (request.currentStock === 0) {
    score += 12;
    rationale.push("Kein Bestand vorhanden, die Bestellung deckt einen Fehlbestand.");
  } else if (signals.stockCoverage < 1) {
    score += 8;
    rationale.push("Bestand deckt nicht die angeforderte Menge.");
  } else if (signals.stockCoverage < 2) {
    score += 4;
    rationale.push("Bestand knapp, Nachbestellung plausibel.");
  } else {
    rationale.push("Bestand deckt die Menge bereits mehrfach.");
  }

  if (signals.quantityRatio !== null) {
    if (signals.quantityRatio <= 2) {
      score += 10;
      rationale.push(
        `Menge liegt im ueblichen Rahmen (${signals.quantityRatio}x des Durchschnitts aus ${signals.historySamples} Vorgaengen).`,
      );
    } else if (signals.quantityRatio > 3) {
      score -= 18;
      risks.push(
        `Menge ist ${signals.quantityRatio}x so hoch wie der Durchschnitt der letzten 90 Tage.`,
      );
    }
  } else {
    rationale.push("Zu wenig Vergleichsdaten fuer eine Mengenbewertung.");
  }

  if (signals.justificationLength < 20) {
    score -= 8;
    risks.push("Begruendung ist kuerzer als 20 Zeichen.");
  } else {
    rationale.push("Begruendung ist ausreichend dokumentiert.");
  }

  if (duplicatePending > 0) {
    score -= 10;
    risks.push(`${duplicatePending} weitere offene Anforderung fuer dasselbe Produkt.`);
  }

  if (signals.historyRejectionRate !== null && signals.historyRejectionRate > 0.5) {
    score -= 10;
    risks.push("Mehr als die Haelfte vergleichbarer Anforderungen wurde abgelehnt.");
  }

  const clamped = Math.max(0, Math.min(100, Math.round(score)));
  const decision: Decision = clamped >= 70 ? "approve" : clamped >= 40 ? "review" : "reject";

  const observed =
    1 +
    Number(signals.quantityRatio !== null) +
    Number(signals.justificationLength > 0) +
    Number(signals.historySamples > 0);
  const confidence = round(Math.min(0.35 + observed * 0.12, 0.95));

  return {
    requestId: request.id,
    score: clamped,
    decision,
    confidence,
    rationale,
    risks,
    signals,
  };
}
