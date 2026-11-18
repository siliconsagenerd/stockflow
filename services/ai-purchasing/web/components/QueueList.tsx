import type { QueueItem } from "../api";
import type { Locale, MessageKey } from "../i18n";
import { translate } from "../i18n";
import { DecisionBadge } from "./DecisionBadge";

function formatAge(iso: string, locale: Locale): string {
  const days = Math.round((Date.now() - new Date(iso).getTime()) / 86400000);
  if (days <= 0) return translate(locale, "relative.today");
  if (days === 1) return translate(locale, "relative.yesterday");
  return translate(locale, "relative.days", { n: days });
}

export function formatQuantity(value: number, locale: Locale): string {
  return new Intl.NumberFormat(locale === "de" ? "de-DE" : "en-GB").format(value);
}

interface Props {
  items: QueueItem[];
  selectedId: string | null;
  locale: Locale;
  onSelect: (id: string) => void;
}

export function QueueList({ items, selectedId, locale, onSelect }: Props) {
  return (
    <ul className="divide-y divide-zinc-200">
      {items.map(({ request, recommendation }) => {
        const selected = request.id === selectedId;
        return (
          <li key={request.id}>
            <button
              type="button"
              onClick={() => onSelect(request.id)}
              aria-current={selected ? "true" : undefined}
              className={`flex w-full flex-col gap-1.5 px-4 py-3 text-left transition-colors focus-visible:-outline-offset-2 ${
                selected
                  ? "border-l-2 border-accent bg-accent-soft"
                  : "border-l-2 border-transparent hover:bg-zinc-50"
              }`}
            >
              <span className="flex items-start justify-between gap-3">
                <span className="text-[13px] font-medium leading-snug text-zinc-900">
                  {request.productName}
                </span>
                <DecisionBadge decision={recommendation.decision} locale={locale} />
              </span>
              <span className="flex items-center justify-between gap-3 text-[11px] text-zinc-500">
                <span className="truncate">
                  {request.sku} &middot; {formatQuantity(request.quantity, locale)} {request.unit}
                </span>
                <span className="shrink-0 tabular-nums">
                  {formatAge(request.createdAt, locale)}
                </span>
              </span>
              <span className="flex items-center justify-between gap-3 text-[11px] text-zinc-400">
                <span className="truncate">{request.requester}</span>
                <span className="shrink-0 font-mono tabular-nums text-zinc-500">
                  {recommendation.score}
                  <span className="text-zinc-400">/100</span>
                </span>
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}
