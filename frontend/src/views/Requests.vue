<template>
  <section>
    <h1>{{ t("nav.requests") }}</h1>
    <p class="muted">{{ t("requests.subtitle") }}</p>

    <div class="card">
      <h2>{{ t("request.create") }}</h2>
      <form @submit.prevent="submit" novalidate>
        <div class="field">
          <label for="product">{{ t("common.product") }}</label>
          <select id="product" v-model="form.productId" required>
            <option value="" disabled>{{ t("request.selectProduct") }}</option>
            <option v-for="p in products" :key="p.id" :value="p.id">
              {{ p.name }} ({{ p.sku }}), {{ p.unit }}
            </option>
          </select>
        </div>
        <div class="field">
          <label for="qty">{{ t("common.quantity") }}</label>
          <input id="qty" type="number" min="1" step="1" v-model.number="form.quantity" required />
        </div>
        <div class="field">
          <label for="just">{{ t("request.justification") }}</label>
          <textarea id="just" v-model="form.justification" maxlength="500" rows="3"></textarea>
          <span class="muted">{{ (form.justification || "").length }}/500</span>
        </div>
        <div class="actions">
          <button type="submit" :disabled="submitting">
            {{ submitting ? t("request.submitting") : t("request.submit") }}
          </button>
          <span v-if="successMsg" class="status-badge status-success" role="status">{{ successMsg }}</span>
          <span v-if="errorMsg" class="status-badge status-danger" role="alert">{{ errorMsg }}</span>
        </div>
      </form>
    </div>

    <div class="card">
      <h2>{{ t("request.listTitle") }}</h2>
      <p v-if="loading" class="muted">{{ t("common.loading") }}</p>
      <p v-else-if="requests.length === 0" class="muted">{{ t("request.empty") }}</p>
      <div class="table-wrap" v-else><table class="table">
        <thead>
          <tr>
            <th>{{ t("common.product") }}</th>
            <th>{{ t("common.quantity") }}</th>
            <th>{{ t("common.status") }}</th>
            <th>{{ t("request.requester") }}</th>
            <th>{{ t("common.date") }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in requests" :key="r.id">
            <td>{{ r.productName }} <span class="muted">({{ r.productSku }})</span></td>
            <td>{{ r.quantity }} {{ r.productUnit }}</td>
            <td><span class="status-badge status-pending">{{ tStatus(r.status) }}</span></td>
            <td>{{ r.requesterName }}</td>
            <td>{{ new Date(r.createdAt).toLocaleString() }}</td>
          </tr>
        </tbody>
      </table></div>
      <p v-if="listError" class="status-badge status-danger">{{ listError }}</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "../lib/i18n";
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
const { t, tStatus, tApi } = useI18n();
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
    listError.value = e instanceof Error ? e.message : t("common.loadFailed");
  } finally {
    loading.value = false;
  }
});

async function loadProducts() {
  const res = await apiFetch("/api/products");
  if (!res.ok) throw new Error(t("request.productsLoadFailed"));
  products.value = await res.json();
}

async function loadRequests() {
  const res = await apiFetch("/api/requests");
  if (!res.ok) throw new Error("Anfragen konnten nicht geladen werden");
  requests.value = await res.json();
}

async function submit() {
  successMsg.value = "";
  errorMsg.value = "";
  if (!form.value.productId) { errorMsg.value = t("request.selectProductError"); return; }
  if (!form.value.quantity || form.value.quantity < 1) { errorMsg.value = t("request.quantityError"); return; }
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
      errorMsg.value = tApi((data && data.error) || `${t("common.error")} (HTTP ${res.status})`);
      return;
    }
    successMsg.value = `${t("request.created")} (${String(data.id).slice(0, 8)}…)`;
    form.value = { productId: "", quantity: 1, justification: "" };
    await loadRequests();
  } catch (e) {
    errorMsg.value = e instanceof Error ? e.message : t("common.networkError");
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