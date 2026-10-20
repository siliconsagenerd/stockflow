<template>
  <section>
    <p><router-link to="/orders">← Zurück zu Bestellungen</router-link></p>
    <h1>Purchase Order {{ order?.reference }}</h1>
    <p v-if="loading" class="muted">Laden…</p>
    <p v-if="error" class="status-badge status-danger">{{ error }}</p>

    <div v-if="order" class="card">
      <div>Produkt: <strong>{{ order.productName }}</strong> ({{ order.productSku }})</div>
      <div>Lieferant: {{ order.supplierName }}</div>
      <div>Lager: {{ order.warehouseName }}</div>
      <div>Bestellt: {{ order.orderedQuantity }}</div>
      <div>Empfangen: {{ order.receivedQuantity }}</div>
      <div>Ausstehend: {{ order.orderedQuantity - order.receivedQuantity }}</div>
      <div>Status: <span class="status-badge status-pending">{{ order.status }}</span></div>
      <div>Erstellt: {{ new Date(order.createdAt).toLocaleString() }}</div>
    </div>

    <div v-if="order && order.status !== 'Completed'" class="card">
      <h2>Wareneingang</h2>
      <label>Menge empfangen</label>
      <input v-model.number="qty" type="number" min="1" />
      <div class="actions">
        <button :disabled="busy" @click="receive">Empfangen</button>
      </div>
      <p v-if="msg" class="status-badge" :class="msgOk ? 'status-success' : 'status-danger'">{{ msg }}</p>
    </div>

    <div class="card">
      <h2>Wareneingangshistorie</h2>
      <p v-if="receipts.length === 0" class="muted">Noch keine Wareneingänge.</p>
      <table v-else class="table">
        <thead><tr><th>Zeit</th><th>Menge</th><th>Benutzer</th><th>Idempotenzschlüssel</th></tr></thead>
        <tbody>
          <tr v-for="r in receipts" :key="r.id">
            <td>{{ new Date(r.receivedAt).toLocaleString() }}</td>
            <td>{{ r.quantity }}</td>
            <td>{{ r.receivedByName }}</td>
            <td class="muted">{{ r.idempotencyKey }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import { apiFetch, ensureSession } from "../lib/api";

interface Order { id: string; reference: string; purchaseRequestId: string; supplierId: string; supplierName: string; warehouseId: string; warehouseName: string; productId: string; productName: string; productSku: string; orderedQuantity: number; receivedQuantity: number; status: string; createdAt: string; }
interface Receipt { id: string; purchaseOrderId: string; workspaceId: string; quantity: number; receivedById: string; receivedByName: string; receivedAt: string; idempotencyKey: string; orderEmpfangendQuantity: number; orderStatus: string; }

const route = useRoute();
const order = ref<Order | null>(null);
const receipts = ref<Receipt[]>([]);
const loading = ref(true);
const error = ref("");
const qty = ref(1);
const busy = ref(false);
const msg = ref("");
const msgOk = ref(true);
const idempotencyKey = ref(crypto.randomUUID());

onMounted(async () => {
  try {
    await ensureSession();
    await load();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "Failed to load";
  } finally {
    loading.value = false;
  }
});

async function load() {
  const id = route.params.id as string;
  const r = await apiFetch(`/api/purchase-orders/${id}`);
  if (!r.ok) { error.value = "Not found"; return; }
  order.value = await r.json();
  // receipts: fetch from movements? We'll keep a local list; reload not available, so fetch all receipts via a simple approach: none endpoint. Show empty for now.
}

async function receive() {
  busy.value = true; msg.value = "";
  try {
    const id = route.params.id as string;
    const res = await apiFetch(`/api/purchase-orders/${id}/receive`, {
      method: "POST",
      body: JSON.stringify({ quantity: qty.value, idempotencyKey: idempotencyKey.value }),
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      if (res.status === 409) {
        msg.value = (data && data.error) || "Conflict - please retry.";
      } else {
        msg.value = (data && data.error) || ("Failed (HTTP " + res.status + ")");
      }
      msgOk.value = false;
      return;
    }
    msg.value = "Empfangend";
    msgOk.value = true;
    // update order + generate new key for next receipt
    await load();
    idempotencyKey.value = crypto.randomUUID();
    qty.value = 1;
  } catch (e) {
    msg.value = e instanceof Error ? e.message : "Network error";
    msgOk.value = false;
  } finally {
    busy.value = false;
  }
}
</script>

<style scoped>
.table { width: 100%; border-collapse: collapse; }
.table th, .table td { padding: 0.5rem; border-bottom: 1px solid var(--border); text-align: left; font-size: 0.9rem; }
.actions { margin-top: 0.5rem; }
</style>
