import { useEffect, useMemo, useState } from "react";
import { fetchQueue, type QueueItem, type QueueResponse } from "./api";
import { readStoredLocale, translate, type Locale } from "./i18n";
import { DecisionPanel } from "./components/DecisionPanel";
import { LanguageToggle } from "./components/LanguageToggle";
import { QueueList } from "./components/QueueList";

type Status = "loading" | "ready" | "error";

export default function App() {
  const [locale, setLocale] = useState<Locale>(() => readStoredLocale());
  const [status, setStatus] = useState<Status>("loading");
  const [data, setData] = useState<QueueResponse | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  const t = (key: Parameters<typeof translate>[1], vars?: Record<string, string | number>) =>
    translate(locale, key, vars);

  useEffect(() => {
    document.documentElement.lang = locale;
    try {
      localStorage.setItem("ai-purchasing.locale", locale);
    } catch {
      // preference is optional
    }
  }, [locale]);

  useEffect(() => {
    let cancelled = false;
    setStatus("loading");
    fetchQueue()
      .then((response) => {
        if (cancelled) return;
        setData(response);
        setStatus("ready");
        setSelectedId((current) => current ?? response.items[0]?.request.id ?? null);
      })
      .catch(() => {
        if (!cancelled) setStatus("error");
      });
    return () => {
      cancelled = true;
    };
  }, [attempt]);

  const items: QueueItem[] = data?.items ?? [];
  const selected = useMemo(
    () => items.find((item) => item.request.id === selectedId) ?? items[0] ?? null,
    [items, selectedId],
  );

  return (
    <div className="min-h-screen bg-zinc-100">
      <header className="sticky top-0 z-10 border-b border-zinc-200 bg-white/95 backdrop-blur">
        <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-3.5 sm:px-6">
          <div className="min-w-0">
            <h1 className="text-[15px] font-semibold tracking-tight text-zinc-900">{t("app.title")}</h1>
            <p className="mt-0.5 text-xs text-zinc-500">{t("app.subtitle")}</p>
          </div>
          <div className="flex items-center gap-2.5">
            <span className="hidden items-center gap-1.5 rounded-full border border-zinc-200 bg-zinc-50 px-2.5 py-1 font-mono text-[11px] text-zinc-600 sm:inline-flex">
              <span
                aria-hidden="true"
                className="h-1.5 w-1.5 rounded-full bg-emerald-500"
              />
              {data ? (data.source === "llm" ? `${t("engine.llm")} · ${data.model}` : t("engine.rules")) : "..."}
            </span>
            <LanguageToggle locale={locale} onChange={setLocale} />
          </div>
        </div>
      </header>

      <main className="mx-auto grid max-w-6xl grid-cols-1 border-zinc-200 bg-white lg:grid-cols-[340px_1fr] lg:border-x">
        <aside className="border-b border-zinc-200 lg:border-b-0 lg:border-r">
          <div className="flex items-center justify-between border-b border-zinc-200 px-4 py-3">
            <h2 className="text-[11px] font-medium uppercase tracking-wide text-zinc-500">
              {t("queue.title", { count: items.length })}
            </h2>
          </div>

          {status === "loading" && (
            <div className="space-y-3 p-4" aria-busy="true" aria-label={t("state.loading")}>
              {[0, 1, 2].map((row) => (
                <div key={row} className="animate-pulse space-y-2">
                  <div className="h-3 w-3/4 rounded bg-zinc-200" />
                  <div className="h-2.5 w-1/2 rounded bg-zinc-100" />
                </div>
              ))}
            </div>
          )}

          {status === "error" && (
            <div className="m-4 rounded-lg border border-rose-200 bg-rose-50 p-3">
              <p className="text-[13px] text-rose-900">{t("state.error")}</p>
              <button
                type="button"
                onClick={() => setAttempt((value) => value + 1)}
                className="mt-2 min-h-9 rounded-md border border-rose-300 bg-white px-3 text-xs font-medium text-rose-900"
              >
                {t("state.retry")}
              </button>
            </div>
          )}

          {status === "ready" && items.length > 0 && (
            <QueueList items={items} selectedId={selected?.request.id ?? null} locale={locale} onSelect={setSelectedId} />
          )}

          {status === "ready" && items.length === 0 && (
            <div className="m-4 rounded-lg border border-dashed border-zinc-300 p-5 text-center">
              <p className="text-[13px] font-medium text-zinc-700">{t("queue.empty")}</p>
              <p className="mt-1 text-xs leading-relaxed text-zinc-500">{t("queue.emptyHint")}</p>
            </div>
          )}
        </aside>

        <section className="bg-zinc-50/60">
          {selected ? (
            <DecisionPanel item={selected} locale={locale} />
          ) : (
            <div className="flex h-full min-h-[320px] items-center justify-center px-6 text-center">
              <p className="max-w-sm text-[13px] leading-relaxed text-zinc-500">{t("queue.emptyHint")}</p>
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
