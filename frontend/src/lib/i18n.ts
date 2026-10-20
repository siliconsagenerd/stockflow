import { ref } from "vue";

export type Locale = "de" | "en";

const locale = ref<Locale>((localStorage.getItem("stockflow.locale") as Locale) || "de");

export function useI18n() {
  const t = (key: string): string => {
    const dict = locale.value === "de" ? de : en;
    return dict[key] ?? key;
  };
  const setLocale = (l: Locale) => {
    locale.value = l;
    localStorage.setItem("stockflow.locale", l);
  };
  const toggle = () => setLocale(locale.value === "de" ? "en" : "de");
  return { locale, t, setLocale, toggle };
}

const de: Record<string, string> = {
  "app.title": "StockFlow",
  "app.subtitle": "Von der Anfrage zum Wareneingang",
  "nav.overview": "Übersicht",
  "nav.requests": "Anfragen",
  "nav.approvals": "Freigaben",
  "nav.orders": "Bestellungen",
  "nav.inventory": "Bestand",
  "lang.toggle": "EN",
  "lang.current": "DE",
  "identity.label": "Demo-Identität",
  "identity.hint": "Demo-Identität – keine sichere Authentifizierung",
  "session.create": "Sitzung erstellen",
  "session.reload": "Neu laden",
  "common.loading": "Laden...",
  "common.empty": "Keine Daten",
  "common.error": "Fehler",
  "common.save": "Speichern",
  "common.cancel": "Abbrechen",
  "common.submit": "Absenden",
  "common.approve": "Freigeben",
  "common.reject": "Ablehnen",
  "common.receive": "Wareneingang",
  "common.quantity": "Menge",
  "common.product": "Produkt",
  "common.status": "Status",
  "common.date": "Datum",
  "overview.pending": "Offene Anfragen",
  "overview.openOrders": "Offene Bestellungen",
  "overview.completed": "Abgeschlossene Bestellungen",
  "overview.receipts": "Wareneingänge",
  "overview.stock": "Produkte mit Bestand",
  "overview.recentActivity": "Letzte Aktivitäten",
  "request.create": "Neue Anfrage",
  "request.justification": "Begründung (optional)",
  "request.submit": "Anfrage absenden",
  "request.validation": "Menge muss eine positive ganze Zahl sein",
  "approval.queue": "Freigabe-Warteschlange",
  "approval.self": "Selbstfreigabe nicht erlaubt",
  "order.partial": "Teilweise empfangen",
  "order.completed": "Abgeschlossen",
  "order.receive6": "6 empfangen",
  "order.receive4": "4 empfangen",
  "inventory.balance": "Bestand",
  "inventory.movements": "Bewegungen",
  "health.connected": "Verbunden",
  "health.unreachable": "Nicht erreichbar",
};

const en: Record<string, string> = {
  "app.title": "StockFlow",
  "app.subtitle": "From Request to Receipt",
  "nav.overview": "Overview",
  "nav.requests": "Requests",
  "nav.approvals": "Approvals",
  "nav.orders": "Orders",
  "nav.inventory": "Inventory",
  "lang.toggle": "DE",
  "lang.current": "EN",
  "identity.label": "Demo identity",
  "identity.hint": "Demo identity — not secure authentication",
  "session.create": "Create session",
  "session.reload": "Reload",
  "common.loading": "Loading...",
  "common.empty": "No data",
  "common.error": "Error",
  "common.save": "Save",
  "common.cancel": "Cancel",
  "common.submit": "Submit",
  "common.approve": "Approve",
  "common.reject": "Reject",
  "common.receive": "Receive",
  "common.quantity": "Quantity",
  "common.product": "Product",
  "common.status": "Status",
  "common.date": "Date",
  "overview.pending": "Pending requests",
  "overview.openOrders": "Open orders",
  "overview.completed": "Completed orders",
  "overview.receipts": "Receipts recorded",
  "overview.stock": "Products with stock",
  "overview.recentActivity": "Recent activity",
  "request.create": "New request",
  "request.justification": "Justification (optional)",
  "request.submit": "Submit request",
  "request.validation": "Quantity must be a positive whole number",
  "approval.queue": "Approval queue",
  "approval.self": "Self-approval not allowed",
  "order.partial": "Partially received",
  "order.completed": "Completed",
  "order.receive6": "Receive 6",
  "order.receive4": "Receive 4",
  "inventory.balance": "Stock",
  "inventory.movements": "Movements",
  "health.connected": "Connected",
  "health.unreachable": "Unreachable",
};
