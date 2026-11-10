import type { Decision } from "../api";
import type { Locale } from "../i18n";
import { translate } from "../i18n";

const styles: Record<Decision, string> = {
  approve: "border-emerald-200 bg-emerald-50 text-emerald-800",
  review: "border-amber-200 bg-amber-50 text-amber-800",
  reject: "border-rose-200 bg-rose-50 text-rose-800",
};

const key = {
  approve: "decision.approve",
  review: "decision.review",
  reject: "decision.reject",
} as const;

export function DecisionBadge({
  decision,
  locale,
  size = "sm",
}: {
  decision: Decision;
  locale: Locale;
  size?: "sm" | "md";
}) {
  return (
    <span
      className={`inline-flex items-center whitespace-nowrap rounded-md border font-medium ${styles[decision]} ${
        size === "sm" ? "px-2 py-0.5 text-[11px]" : "px-2.5 py-1 text-xs"
      }`}
    >
      {translate(locale, key[decision])}
    </span>
  );
}
