<template>
  <section>
    <h1>{{ t("nav.inventory") }}</h1>
    <p class="muted">Workspace-spezifische Bestände und Bewegungshistorie.</p>

    <div class="card">
      <h2>Bestände</h2>
      <p v-if="loading" class="muted">Laden…</p>
      <p v-else-if="balances.length === 0" class="muted">Noch keine Bestände.</p>
      <div class="table-wrap" v-else><table class="table">
        <thead>
          <tr><th>Produkt</th><th>Lager</th><th>Menge</th></tr>
        </thead>
        <tbody>
          <tr v-for="b in balances" :key="b.productId + b.warehouseId">
            <td>{{ b.productName }} <span class="muted">({{ b.productSku }})</span></td>
            <td>{{ b.warehouseName }}</td>
            <td>{{ b.quantity }} {{ b.productUnit }}</td>
          </tr>
        </tbody>
      </table></div>
    </div>

    <div class="card">
      <h2>Bewegungshistorie</h2>
      <p v-if="movements.length === 0" class="muted">Noch keine Bewegungen.</p>
      <div class="table-wrap" v-else><table class="table">
        <thead>
          <tr><th>Zeit</th><th>Produkt</th><th>Änderung</th><th>Grund</th><th>Referenz</th></tr>
        </thead>
        <tbody>
          <tr v-for="m in movements" :key="m.id">
            <td>{{ new Date(m.createdAt).toLocaleString() }}</td>
            <td>{{ m.productName }}</td>
            <td :class="m.delta >= 0 ? 'pos' : 'neg'">{{ m.delta > 0 ? '+' : '' }}{{ m.delta }}</td>
            <td>{{ m.reason }}</td>
            <td class="muted">{{ m.referenceId ?? '—' }}</td>
          </tr>
        </tbody>
      </table></div>
    </div>
    <p v-if="error" class="status-badge status-danger">{{ error }}</p>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "../lib/i18n";
import { apiFetch, ensureSession } from "../lib/api";

interface Balance { productId: string; productName: string; productSku: string; productUnit: string; warehouseId: string; warehouseName: string; quantity: number; }
interface Movement { id: string; productId: string; productName: string; warehouseId: string; warehouseName: string; delta: number; reason: string; referenceId: string | null; createdAt: string; workspaceId: string; }

const balances = ref<Balance[]>([]);
const movements = ref<Movement[]>([]);
const loading = ref(true);
const { t } = useI18n();
const error = ref("");

onMounted(async () => {
  try {
    await ensureSession();
    const b = await apiFetch("/api/inventory"); if (b.ok) balances.value = await b.json();
    const m = await apiFetch("/api/stock-movements"); if (m.ok) movements.value = await m.json();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "Laden fehlgeschlagen";
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.table { width: 100%; border-collapse: collapse; }
.table th, .table td { padding: 0.5rem; border-bottom: 1px solid var(--border); text-align: left; font-size: 0.9rem; }
.pos { color: #16a34a; font-weight: 600; }
.neg { color: #dc2626; font-weight: 600; }
</style>
