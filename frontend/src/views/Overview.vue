<template>
  <section>
    <h1>Overview</h1>
    <p class="muted">StockFlow tracks procurement from request to receipt: request → approval → purchase order → goods receipt → stock.</p>

    <div class="card">
      <h2>Start the demo</h2>
      <ol>
        <li>Switch to <strong>Anna Beispiel (Employee)</strong> and create a request for 10 units.</li>
        <li>Switch to <strong>Markus Beispiel (Manager)</strong> and approve it.</li>
        <li>Switch to <strong>Werner Beispiel (Warehouse)</strong>, open the purchase order, and receive 6.</li>
        <li>Receive the remaining 4 to complete the order.</li>
      </ol>
    </div>

    <div v-if="loading" class="muted">Loading…</div>
    <p v-if="error" class="status-badge status-danger">{{ error }}</p>

    <div v-if="!loading" class="metrics">
      <router-link to="/requests" class="metric card">
        <div class="metric-value">{{ overview?.pendingRequests ?? 0 }}</div>
        <div class="metric-label">Pending requests</div>
      </router-link>
      <router-link to="/orders" class="metric card">
        <div class="metric-value">{{ overview?.openOrders ?? 0 }}</div>
        <div class="metric-label">Open orders (incl. partial)</div>
      </router-link>
      <router-link to="/orders" class="metric card">
        <div class="metric-value">{{ overview?.completedOrders ?? 0 }}</div>
        <div class="metric-label">Completed orders</div>
      </router-link>
      <div class="metric card">
        <div class="metric-value">{{ overview?.receiptsRecorded ?? 0 }}</div>
        <div class="metric-label">Receipts recorded</div>
      </div>
      <div class="metric card">
        <div class="metric-value">{{ overview?.productsWithStock ?? 0 }}</div>
        <div class="metric-label">Products with stock</div>
      </div>
    </div>

    <div class="card">
      <h2>Recent activity</h2>
      <p v-if="!overview?.recentActivity?.length" class="muted">No activity yet. Create a request to begin.</p>
      <ul v-else class="activity">
        <li v-for="(a, i) in overview.recentActivity" :key="i">
          <span class="muted">{{ new Date(a.at).toLocaleString() }}</span> — {{ a.label }} <span class="status-badge status-pending">{{ a.status }}</span>
        </li>
      </ul>
    </div>

    <div class="card">
      <h2>How it works</h2>
      <p>Request → Approval → Purchase Order → Goods Receipt → Stock. Each step is persisted per workspace; demo identities are simulated, not secure authentication.</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { apiFetch, ensureSession } from "../lib/api";

const overview = ref<any>(null);
const loading = ref(true);
const error = ref("");

onMounted(async () => {
  try {
    await ensureSession();
    const res = await apiFetch("/api/overview");
    if (!res.ok) throw new Error("Failed to load overview");
    overview.value = await res.json();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "Failed to load";
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 0.75rem; margin: 1rem 0; }
.metric { text-decoration: none; color: inherit; text-align: center; }
.metric-value { font-size: 2rem; font-weight: 700; }
.metric-label { font-size: 0.85rem; color: var(--muted); }
.activity { list-style: none; padding: 0; }
.activity li { padding: 0.4rem 0; border-bottom: 1px solid var(--border); }
</style>
