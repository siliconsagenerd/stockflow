<template>
  <section>
    <h1>Requests</h1>
    <p class="muted">Create material requests. Each is persisted as Pending for the current demo workspace.</p>

    <div class="card">
      <h2>New request</h2>
      <form @submit.prevent="submit" novalidate>
        <div class="field">
          <label for="product">Product</label>
          <select id="product" v-model="form.productId" required>
            <option value="" disabled>Select a product…</option>
            <option v-for="p in products" :key="p.id" :value="p.id">
              {{ p.name }} ({{ p.sku }}) — {{ p.unit }}
            </option>
          </select>
        </div>
        <div class="field">
          <label for="qty">Quantity</label>
          <input id="qty" type="number" min="1" step="1" v-model.number="form.quantity" required />
        </div>
        <div class="field">
          <label for="just">Justification (optional)</label>
          <textarea id="just" v-model="form.justification" maxlength="500" rows="3"></textarea>
          <span class="muted">{{ (form.justification || "").length }}/500</span>
        </div>
        <div class="actions">
          <button type="submit" :disabled="submitting">
            {{ submitting ? "Submitting…" : "Submit request" }}
          </button>
          <span v-if="successMsg" class="status-badge status-success" role="status">{{ successMsg }}</span>
          <span v-if="errorMsg" class="status-badge status-danger" role="alert">{{ errorMsg }}</span>
        </div>
      </form>
    </div>

    <div class="card">
      <h2>Requests in this workspace</h2>
      <p v-if="loading" class="muted">Loading…</p>
      <p v-else-if="requests.length === 0" class="muted">No requests yet.</p>
      <table v-else class="table">
        <thead>
          <tr>
            <th>Product</th>
            <th>Qty</th>
            <th>Status</th>
            <th>Requester</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in requests" :key="r.id">
            <td>{{ r.productName }} <span class="muted">({{ r.productSku }})</span></td>
            <td>{{ r.quantity }} {{ r.productUnit }}</td>
            <td><span class="status-badge status-pending">{{ r.status }}</span></td>
            <td>{{ r.requesterName }}</td>
            <td>{{ new Date(r.createdAt).toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="listError" class="status-badge status-danger">{{ listError }}</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { apiFetch, ensureSession } from "../lib/api";

interface Product { id: string; sku: string; name: string; unit: string; currentStock: number; }
interface RequestRow {
  id: string; productId: string; productName: string; productSku: string; productUnit: string;
  quantity: number; justification?: string | null; status: string; requesterId: string;
  requesterName: string; createdAt: string; workspaceId: string;
}

const products = ref<Product[]>([]);
const requests = ref<RequestRow[]>([]);
const loading = ref(true);
const listError = ref("");
const submitting = ref(false);
const successMsg = ref("");
const errorMsg = ref("");

const form = ref<{ productId: string; quantity: number | null; justification: string }>({
  productId: "",
  quantity: 1,
  justification: "",
});

onMounted(async () => {
  try {
    await ensureSession();
    await loadProducts();
    await loadRequests();
  } catch (e) {
    listError.value = e instanceof Error ? e.message : "Failed to load";
  } finally {
    loading.value = false;
  }
});

async function loadProducts() {
  const res = await apiFetch("/api/products");
  if (!res.ok) throw new Error("Failed to load products");
  products.value = await res.json();
}

async function loadRequests() {
  const res = await apiFetch("/api/requests");
  if (!res.ok) throw new Error("Failed to load requests");
  requests.value = await res.json();
}

async function submit() {
  successMsg.value = "";
  errorMsg.value = "";
  if (!form.value.productId) { errorMsg.value = "Select a product."; return; }
  if (!form.value.quantity || form.value.quantity < 1) { errorMsg.value = "Quantity must be a positive whole number."; return; }
  submitting.value = true;
  try {
    const res = await apiFetch("/api/requests", {
      method: "POST",
      body: JSON.stringify({
        productId: form.value.productId,
        quantity: form.value.quantity,
        justification: form.value.justification || null,
      }),
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      errorMsg.value = (data && data.error) || `Request failed (HTTP ${res.status})`;
      return;
    }
    successMsg.value = `Request created (${String(data.id).slice(0, 8)}…)`;
    form.value = { productId: "", quantity: 1, justification: "" };
    await loadRequests();
  } catch (e) {
    errorMsg.value = e instanceof Error ? e.message : "Network error";
  } finally {
    submitting.value = false;
  }
}
</script>

<style scoped>
.field { margin-bottom: 0.75rem; display: flex; flex-direction: column; gap: 0.25rem; }
.field label { font-weight: 600; font-size: 0.9rem; }
.field input, .field select, .field textarea {
  padding: 0.5rem 0.6rem; border: 1px solid var(--border); border-radius: var(--radius);
  font: inherit; background: var(--surface);
}
.actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
button {
  background: var(--accent); color: #fff; border: none; padding: 0.5rem 1rem;
  border-radius: var(--radius); font: inherit; font-weight: 600; cursor: pointer;
}
button:disabled { opacity: 0.6; cursor: not-allowed; }
.table { width: 100%; border-collapse: collapse; }
.table th, .table td { padding: 0.5rem; border-bottom: 1px solid var(--border); text-align: left; font-size: 0.9rem; }
</style>