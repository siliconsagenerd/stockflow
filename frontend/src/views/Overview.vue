<template>
  <section>
    <h1>{{ t("nav.overview") }}</h1>
    <p class="muted">StockFlow: {{ t("app.subtitle") }}, {{ t("overview.flow") }}.</p>
    <div class="card">
      <h2>{{ t("overview.demoStart") }}</h2>
      <ol>
        <li>{{ t("overview.step1") }}</li>
        <li>{{ t("overview.step2") }}</li>
        <li>{{ t("overview.step3") }}</li>
        <li>{{ t("overview.step4") }}</li>
      </ol>
    </div>
    <div v-if="loading" class="metrics">
      <div v-for="n in 5" :key="n" class="metric card">
        <div class="skeleton metric-value">&nbsp;</div>
        <div class="skeleton metric-label">&nbsp;</div>
      </div>
    </div>
    <p v-if="error" class="status-badge status-danger">{{ error }}</p>
    <div v-if="!loading" class="metrics">
      <router-link to="/requests" class="metric card">
        <div class="metric-value">{{ overview?.pendingRequests ?? 0 }}</div>
        <div class="metric-label">{{ t("overview.pending") }}</div>
      </router-link>
      <router-link to="/orders" class="metric card">
        <div class="metric-value">{{ overview?.openOrders ?? 0 }}</div>
        <div class="metric-label">{{ t("overview.openOrders") }}</div>
      </router-link>
      <router-link to="/orders" class="metric card">
        <div class="metric-value">{{ overview?.completedOrders ?? 0 }}</div>
        <div class="metric-label">{{ t("overview.completed") }}</div>
      </router-link>
      <div class="metric card">
        <div class="metric-value">{{ overview?.receiptsRecorded ?? 0 }}</div>
        <div class="metric-label">{{ t("overview.receipts") }}</div>
      </div>
      <div class="metric card">
        <div class="metric-value">{{ overview?.productsWithStock ?? 0 }}</div>
        <div class="metric-label">{{ t("overview.stock") }}</div>
      </div>
    </div>
    <div class="card">
      <h2>{{ t("overview.recentActivity") }}</h2>
      <div v-if="!overview?.recentActivity?.length" class="empty-state">
        <p class="muted">{{ t("overview.noActivity") }}</p>
        <router-link to="/requests" class="btn-primary">{{ t("nav.requests") }} →</router-link>
      </div>
      <ul v-else class="activity">
        <li v-for="(a, i) in overview.recentActivity" :key="i">
          <span class="muted">{{ new Date(a.at).toLocaleString() }}</span>, {{ a.label }} <span class="status-badge status-ok">{{ tStatus(a.status) }}</span>
        </li>
      </ul>
    </div>
    <div class="card">
      <h2>{{ t("overview.howItWorks") }}</h2>
      <p>{{ t("overview.howItWorksBody") }}</p>
    </div>
  </section>
</template>
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { apiFetch, ensureSession } from "../lib/api";
import { useI18n } from "../lib/i18n";
const { t, tStatus } = useI18n();
const overview = ref<any>(null);
const loading = ref(true);
const error = ref("");
onMounted(async () => {
  try {
    await ensureSession();
    const res = await apiFetch("/api/overview");
    if (!res.ok) throw new Error(t("overview.loadFailed"));
    overview.value = await res.json();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "Laden fehlgeschlagen";
  } finally {
    loading.value = false;
  }
});
</script>
<style scoped>
.metrics { display: grid; }
.metric { text-decoration: none; color: inherit; }
.metric-value { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.activity { list-style: none; padding: 0; }
.activity li { padding: 0.5rem 0; border-bottom: 1px solid var(--border); }
</style>
