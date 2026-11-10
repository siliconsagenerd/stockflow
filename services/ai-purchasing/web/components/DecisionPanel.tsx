import { useState } from "react";
import type { QueueItem } from "../api";
import type { Locale, MessageKey } from "../i18n";
import { translate } from "../i18n";
import { DecisionBadge } from "./DecisionBadge";
import { formatQuantity } from "./QueueList";

interface Props {
  item: QueueItem;
  locale: Locale;
}

function signalValue(
  locale: Locale,
  key: MessageKey,
  vars?: Record<string, string | number>,
): string {
  return translate(locale, key, vars);
}

export function DecisionPanel({ item, locale }: Props) {
  const [copied, setCopied] = useState(false);
  const { request, recommendation } = item;
  const signals = recommendation.signals;

  const copyNote = async () => {
    const lines = [
      `${request.productName} (${request.sku}), ${formatQuantity(request.quantity, locale)} ${request.unit}`,
      `${translate(locale, "score.label")}: ${recommendation.score}/100`,
      `${translate(locale, "decision.approve")} / ${translate(locale, "decision.review")} / ${translate(locale, "decision.reject")}: ${translate(locale, `decision.${recommendation.decision}` as MessageKey)}`,
      "",
      ...recommendation.rationale.map((line) => `- ${line}`),
      ...recommendation.risks.map((line) => `! ${line}`),
    ];
    try {
      await navigator.clipboard.writeText(lines.join("\n"));
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  };

  const coverage = Math.round(recommendation.confidence * 100);

  return (
    <div className="mx-auto max-w-3xl px-5 py-6 sm:px-8 sm:py-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold tracking-tight text-zinc-900">{request.productName}</h2>
          <p className="mt-1 text-xs text-zinc-500">
            {request.sku} &middot; {translate(locale, "meta.quantity", {
              qty: formatQuantity(request.quantity, locale),
              unit: request.unit,
            })}{" "}
            &middot; {translate(locale, "meta.requester", { name: request.requester })}
          </p>
        </div>
        <span className="rounded-md border border-zinc-200 bg-white px-2 py-1 font-mono text-[11px] text-zinc-600">
          {request.sku}
        </span>
      </div>

      <section className="mt-5 rounded-xl border border-zinc-200 bg-white p-5">
        <div className="grid gap-5 sm:grid-cols-[auto_1fr] sm:items-center">
          <div>
            <p className="text-[11px] font-medium uppercase tracking-wide text-zinc-500">
              {translate(locale, "score.label")}
            </p>
            <p className="mt-1 flex items-baseline gap-1.5">
              <span className="text-4xl font-semibold leading-none text-zinc-900">{recommendation.score}</span>
              <span className="text-xs text-zinc-500">{translate(locale, "score.outOf")}</span>
            </p>
            <div className="mt-2">
              <DecisionBadge decision={recommendation.decision} locale={locale} size="md" />
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-baseline justify-between gap-3">
              <span className="text-[11px] font-medium uppercase tracking-wide text-zinc-500">
                {translate(locale, "confidence.label")}
              </span>
              <span className="text-xs tabular-nums text-zinc-600">{coverage}%</span>
            </div>
            <div
              role="progressbar"
              aria-valuenow={coverage}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-label={translate(locale, "confidence.label")}
              className="h-1.5 w-full overflow-hidden rounded-full bg-zinc-100"
            >
              <div className="h-full rounded-full bg-accent" style={{ width: `${coverage}%` }} />
            </div>
            <p className="text-[11px] text-zinc-500">
              {translate(locale, "confidence.signals", { count: signals.historySamples })}
            </p>
          </div>
        </div>
      </section>

      <section className="mt-6">
        <h3 className="text-[11px] font-medium uppercase tracking-wide text-zinc-500">
          {translate(locale, "section.signals")}
        </h3>
        <dl className="mt-3 grid gap-px overflow-hidden rounded-lg border border-zinc-200 bg-zinc-200 sm:grid-cols-4">
          <div className="bg-white px-3 py-2.5">
            <dt className="text-[11px] text-zinc-500">{translate(locale, "signals.stock")}</dt>
            <dd className="mt-0.5 text-sm font-medium tabular-nums text-zinc-900">
              {signalValue(locale, "signals.stockValue", { value: signals.stockCoverage })}
            </dd>
          </div>
          <div className="bg-white px-3 py-2.5">
            <dt className="text-[11px] text-zinc-500">{translate(locale, "signals.ratio")}</dt>
            <dd className="mt-0.5 text-sm font-medium tabular-nums text-zinc-900">
              {signals.quantityRatio === null
                ? translate(locale, "signals.none")
                : signalValue(locale, "signals.ratioValue", { value: signals.quantityRatio })}
            </dd>
          </div>
          <div className="bg-white px-3 py-2.5">
            <dt className="text-[11px] text-zinc-500">{translate(locale, "signals.justification")}</dt>
            <dd className="mt-0.5 text-sm font-medium tabular-nums text-zinc-900">
              {signalValue(locale, "signals.justValue", { value: signals.justificationLength })}
            </dd>
          </div>
          <div className="bg-white px-3 py-2.5">
            <dt className="text-[11px] text-zinc-500">{translate(locale, "signals.duplicates")}</dt>
            <dd className="mt-0.5 text-sm font-medium tabular-nums text-zinc-900">
              {signals.duplicatePending}
            </dd>
          </div>
        </dl>
      </section>

      <section className="mt-6">
        <h3 className="text-[11px] font-medium uppercase tracking-wide text-zinc-500">
          {translate(locale, "section.rationale")}
        </h3>
        <ul className="mt-2.5 space-y-1.5">
          {recommendation.rationale.map((line) => (
            <li key={line} className="flex gap-2.5 text-[13px] leading-relaxed text-zinc-700">
              <span aria-hidden="true" className="mt-2 h-1 w-1 shrink-0 rounded-full bg-accent" />
              <span>{line}</span>
            </li>
          ))}
        </ul>
      </section>

      {recommendation.risks.length > 0 && (
        <section className="mt-6 rounded-lg border border-rose-200 bg-rose-50/70 p-3.5">
          <h3 className="text-[11px] font-medium uppercase tracking-wide text-rose-800">
            {translate(locale, "section.risks")}
          </h3>
          <ul className="mt-2 space-y-1.5">
            {recommendation.risks.map((line) => (
              <li key={line} className="text-[13px] leading-relaxed text-rose-900">
                {line}
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="mt-7 flex flex-wrap items-center gap-2.5">
        <button
          type="button"
          onClick={copyNote}
          className="min-h-10 rounded-lg bg-accent px-4 text-sm font-medium text-white transition-colors hover:bg-teal-800"
        >
          {copied ? translate(locale, "action.copied") : translate(locale, "action.copy")}
        </button>
        <a
          href={`http://127.0.0.1:5080/requests`}
          target="_blank"
          rel="noreferrer"
          className="inline-flex min-h-10 items-center rounded-lg border border-zinc-300 bg-white px-4 text-sm font-medium text-zinc-700 transition-colors hover:bg-zinc-50"
        >
          {translate(locale, "action.open")}
        </a>
        <span className="text-[11px] leading-snug text-zinc-500">{translate(locale, "note.writePath")}</span>
      </div>
    </div>
  );
}
