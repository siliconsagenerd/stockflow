export type Locale = "de" | "en";

export const messages = {
  de: {
    "app.title": "Einkaufs-Freigabe-Assistent",
    "app.subtitle": "KI-Empfehlungen für offene Bestellanforderungen",
    "lang.label": "Sprache wechseln",
    "lang.de": "DE",
    "lang.en": "EN",
    "queue.title": "Offene Anforderungen ({count})",
    "queue.empty": "Keine offenen Anforderungen",
    "queue.emptyHint": "Sobald jemand eine Bestellung anfragt, erscheint sie hier mit Empfehlung.",
    "relative.today": "heute",
    "relative.yesterday": "gestern",
    "relative.days": "{n} Tage alt",
    "state.loading": "Anforderungen werden geladen",
    "state.error": "Daten konnten nicht geladen werden.",
    "state.retry": "Erneut versuchen",
    "decision.approve": "Freigeben",
    "decision.review": "Prüfen",
    "decision.reject": "Ablehnen",
    "score.label": "Empfehlungswert",
    "score.outOf": "von 100",
    "confidence.label": "Datenlage",
    "confidence.signals": "{count} Signale ausgewertet",
    "section.signals": "Signale",
    "section.rationale": "Begründung der Empfehlung",
    "section.risks": "Auffaelligkeiten",
    "signals.stock": "Bestandsdeckung",
    "signals.ratio": "Menge vs. 90-Tage-Schnitt",
    "signals.justification": "Begründungstext",
    "signals.duplicates": "Weitere offene Anfragen",
    "signals.rejectionRate": "Ablehnungsquote",
    "signals.stockValue": "{value}x der Menge",
    "signals.ratioValue": "{value}x",
    "signals.justValue": "{value} Zeichen",
    "signals.samples": "{count} Vergleichsfaelle",
    "signals.none": "keine Daten",
    "action.copy": "Empfehlung kopieren",
    "action.copied": "Kopiert",
    "action.open": "In StockFlow entscheiden",
    "note.writePath": "Freigaben bleiben im StockFlow-Backend. Dieser Assistent schlägt vor, entscheidet nicht.",
    "engine.rules": "Regelwerk",
    "engine.llm": "LLM",
    "meta.quantity": "Menge {qty} {unit}",
    "meta.requester": "Angefragt von {name}",
  },
  en: {
    "app.title": "Purchasing Approval Assistant",
    "app.subtitle": "AI recommendations for pending purchase requests",
    "lang.label": "Switch language",
    "lang.de": "DE",
    "lang.en": "EN",
    "queue.title": "Pending requests ({count})",
    "queue.empty": "No pending requests",
    "queue.emptyHint": "New purchase requests appear here with a recommendation.",
    "relative.today": "today",
    "relative.yesterday": "yesterday",
    "relative.days": "{n} days old",
    "state.loading": "Loading requests",
    "state.error": "Could not load data.",
    "state.retry": "Try again",
    "decision.approve": "Approve",
    "decision.review": "Review",
    "decision.reject": "Reject",
    "score.label": "Recommendation score",
    "score.outOf": "of 100",
    "confidence.label": "Evidence",
    "confidence.signals": "{count} signals evaluated",
    "section.signals": "Signals",
    "section.rationale": "Why this recommendation",
    "section.risks": "Flags",
    "signals.stock": "Stock coverage",
    "signals.ratio": "Quantity vs. 90-day average",
    "signals.justification": "Justification text",
    "signals.duplicates": "Other open requests",
    "signals.rejectionRate": "Rejection rate",
    "signals.stockValue": "{value}x the quantity",
    "signals.ratioValue": "{value}x",
    "signals.justValue": "{value} characters",
    "signals.samples": "{count} comparable cases",
    "signals.none": "no data",
    "action.copy": "Copy recommendation",
    "action.copied": "Copied",
    "action.open": "Decide in StockFlow",
    "note.writePath": "Approvals stay in the StockFlow backend. This assistant recommends, it does not decide.",
    "engine.rules": "Rule engine",
    "engine.llm": "LLM",
    "meta.quantity": "Quantity {qty} {unit}",
    "meta.requester": "Requested by {name}",
  },
} as const;

export type MessageKey = keyof (typeof messages)["de"];

export function translate(
  locale: Locale,
  key: MessageKey,
  vars: Record<string, string | number> = {},
): string {
  const template: string = messages[locale][key] ?? messages.de[key] ?? key;
  return Object.entries(vars).reduce<string>(
    (text, [name, value]) => text.replaceAll(`{${name}}`, String(value)),
    template,
  );
}

export function readStoredLocale(): Locale {
  try {
    const stored = localStorage.getItem("ai-purchasing.locale");
    if (stored === "de" || stored === "en") return stored;
  } catch {
    // storage can be blocked; German stays the default
  }
  return "de";
}
