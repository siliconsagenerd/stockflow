<template>
  <section>
    <h1>{{ t("nav.orders") }}</h1>
    <p class="muted">{{ t("orders.subtitle") }}</p>

    <div class="card">
      <p v-if="loading" class="muted">{{ t("common.loading") }}</p>
      <p v-else-if="orders.length === 0" class="muted">{{ t("orders.empty") }}</p>
      <div class="table-wrap" v-else><table class="table">
        <thead>
          <tr>
            <th>{{ t("orders.reference") }}</th>
            <th>{{ t("common.product") }}</th>
            <th>{{ t("orders.supplier") }}</th>
            <th>{{ t("common.quantity") }}</th>
            <th>{{ t("common.status") }}</th>
            <th>{{ t("common.date") }}</th>
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
    if (!res.ok) throw new Error(t("orders.loadFailed"));
    orders.value = await res.json();
  } catch (e) {
    error.value = e instanceof Error ? e.message : t("common.loadFailed");
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.table { width: 100%; border-collapse: collapse; }
.table th, .table td { padding: 0.5rem; border-bottom: 1px solid var(--border); text-align: left; font-size: 0.9rem; }
</style>
