import type { Locale } from "../i18n";
import { translate } from "../i18n";

interface Props {
  locale: Locale;
  onChange: (locale: Locale) => void;
}

export function LanguageToggle({ locale, onChange }: Props) {
  const options: Locale[] = ["de", "en"];
  return (
    <div
      role="group"
      aria-label={translate(locale, "lang.label")}
      className="flex items-center rounded-full border border-zinc-300 bg-white/80 p-0.5 shadow-[0_1px_0_rgba(0,0,0,0.04)] backdrop-blur"
    >
      {options.map((option) => {
        const active = option === locale;
        return (
          <button
            key={option}
            type="button"
            onClick={() => onChange(option)}
            aria-pressed={active}
            aria-label={option === "de" ? "Deutsch" : "English"}
            className={`min-h-8 min-w-11 rounded-full px-3 text-xs font-medium tracking-wide transition-colors ${
              active
                ? "bg-accent text-white"
                : "text-zinc-600 hover:text-zinc-900"
            }`}
          >
            {translate(locale, option === "de" ? "lang.de" : "lang.en")}
          </button>
        );
      })}
    </div>
  );
}
