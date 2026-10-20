<template>
  <section>
    <h1>{{ t("nav.orders") }}</h1>
    <p class="muted">Bestellungen aus freigegebenen Anfragen in diesem Workspace.</p>

    <div class="card">
      <p v-if="loading" class="muted">Laden…</p>
      <p v-else-if="orders.length === 0" class="muted">Noch keine Bestellungen.</p>
      <div class="table-wrap" v-else><table class="table">
        <thead>
          <tr>
            <th>Referenz</th>
            <th>Produkt</th>
            <th>Lieferant</th>
            <th>Menge</th>
            <th>Status</th>
            <th>Erstellt</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="o in orders" :key="o.id">
            <td><router-link :to="`/orders/${o.id}`">{{ o.reference }}</router-link></td>
            <td>{{ o.productName }} <span class="muted">({{ o.productSku }})</span></td>
            <td>{{ o.supplierName }}</td>
            <td>{{ o.orderedQuantity }}</td>
            <td><span class="status-badge status-pending">{{ tStatus(o.status) }}</span></td>
            <td>{{ new Date(o.createdAt).toLocaleString() }}</td>
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

interface OrderRow { id: string; reference: string; purchaseRequestId: string; supplierId: string; supplierName: string; warehouseId: string; warehouseName: string; productId: string; productName: string; productSku: string; orderedQuantity: number; receivedQuantity: number; status: string; createdAt: string; }

const orders = ref<OrderRow[]>([]);
const loading = ref(true);
const { t, tStatus } = useI18n();
const error = ref("");

onMounted(async () => {
  try {
    await ensureSession();
    const res = await apiFetch("/api/purchase-orders");
    if (!res.ok) throw new Error("Bestellungen konnten nicht geladen werden");
    orders.value = await res.json();
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
</style>
