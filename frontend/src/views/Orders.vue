<template>
  <section>
    <h1>Purchase Orders</h1>
    <p class="muted">Orders created from approved requests in this workspace.</p>

    <div class="card">
      <p v-if="loading" class="muted">Loading…</p>
      <p v-else-if="orders.length === 0" class="muted">No purchase orders yet.</p>
      <table v-else class="table">
        <thead>
          <tr>
            <th>Reference</th>
            <th>Product</th>
            <th>Supplier</th>
            <th>Qty</th>
            <th>Status</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="o in orders" :key="o.id">
            <td><router-link :to="`/orders/${o.id}`">{{ o.reference }}</router-link></td>
            <td>{{ o.productName }} <span class="muted">({{ o.productSku }})</span></td>
            <td>{{ o.supplierName }}</td>
            <td>{{ o.orderedQuantity }}</td>
            <td><span class="status-badge status-pending">{{ o.status }}</span></td>
            <td>{{ new Date(o.createdAt).toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <p v-if="error" class="status-badge status-danger">{{ error }}</p>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { apiFetch, ensureSession } from "../lib/api";

interface OrderRow { id: string; reference: string; purchaseRequestId: string; supplierId: string; supplierName: string; warehouseId: string; warehouseName: string; productId: string; productName: string; productSku: string; orderedQuantity: number; receivedQuantity: number; status: string; createdAt: string; }

const orders = ref<OrderRow[]>([]);
const loading = ref(true);
const error = ref("");

onMounted(async () => {
  try {
    await ensureSession();
    const res = await apiFetch("/api/purchase-orders");
    if (!res.ok) throw new Error("Failed to load orders");
    orders.value = await res.json();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "Failed to load";
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.table { width: 100%; border-collapse: collapse; }
.table th, .table td { padding: 0.5rem; border-bottom: 1px solid var(--border); text-align: left; font-size: 0.9rem; }
</style>
