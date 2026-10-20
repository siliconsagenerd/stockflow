<template>
  <div class="app-shell">
    <header class="app-header">
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">SF</span>
        <span>StockFlow</span>
        <span class="muted">{{ t("app.subtitle") }}</span>
      </div>
      <div class="header-actions">
        <nav class="nav" aria-label="Main navigation">
          <router-link to="/">{{ t("nav.overview") }}</router-link>
          <router-link to="/requests">{{ t("nav.requests") }}</router-link>
          <router-link to="/approvals">{{ t("nav.approvals") }}</router-link>
          <router-link to="/orders">{{ t("nav.orders") }}</router-link>
          <router-link to="/inventory">{{ t("nav.inventory") }}</router-link>
        </nav>
        <button class="lang-toggle" @click="toggle" :aria-label="locale === 'de' ? 'Switch to English' : 'Auf Deutsch wechseln'">
          {{ t("lang.toggle") }}
        </button>
      </div>
    </header>
    <main class="app-main">
      <DemoIdentitySwitcher />
      <ConnectivityBadge />
      <router-view />
    </main>
  </div>
</template>
<script setup lang="ts">
import { onMounted } from "vue";
import ConnectivityBadge from "./components/ConnectivityBadge.vue";
import DemoIdentitySwitcher from "./components/DemoIdentitySwitcher.vue";
import { ensureSession } from "./lib/api";
import { useI18n } from "./lib/i18n";
const { t, locale, toggle } = useI18n();
onMounted(() => {
  ensureSession().catch(() => {});
});
</script>
<style scoped>
.header-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
.lang-toggle {
  padding: 0.3rem 0.6rem;
  border: 1px solid var(--border, #ccc);
  border-radius: var(--radius, 4px);
  background: var(--surface, #fff);
  color: var(--text, #111);
  cursor: pointer;
  font-weight: 600;
  font-size: 0.8rem;
  transition: background-color 0.15s ease, border-color 0.15s ease;
}
.lang-toggle:hover { background: var(--surface-2, #eee); border-color: var(--border-strong, #bbb); }
</style>
