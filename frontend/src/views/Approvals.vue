<template>
  <section>
    <h1>{{ t("nav.approvals") }}</h1>
    <p class="muted">Ausstehende Anfragen in diesem Workspace. Nur eine Manager-Demo-Identität kann entscheiden.</p>
    <p v-if="!isManager" class="status-badge status-warning">Zu einer Manager-Identität wechseln, um freizugeben oder abzulehnen.</p>

    <p v-if="pageMsg" class="status-badge" :class="pageMsg.ok ? 'status-success' : 'status-danger'" style="margin-bottom:0.75rem;">{{ pageMsg.text }}</p>
    <div class="card">
      <p v-if="loading" class="muted">Laden…</p>
      <p v-else-if="pending.length === 0" class="muted">Keine ausstehenden Anfragen.</p>
      <div v-else v-for="r in pending" :key="r.id" class="card" style="margin-bottom:0.75rem;">
        <strong>{{ r.productName }}</strong> <span class="muted">({{ r.productSku }})</span>
        <div>Menge: {{ r.quantity }} {{ r.productUnit }}</div>
        <div>Anfragender: {{ r.requesterName }}</div>
        <div>Erstellt: {{ new Date(r.createdAt).toLocaleString() }}</div>
        <div v-if="r.justification" class="muted">Begründung: {{ r.justification }}</div>
        <div class="actions" style="margin-top:0.5rem;">
          <button v-if="isManager" :disabled="busy" @click="decide(r.id, 'approve')">Freigeben</button>
          <button v-if="isManager" class="secondary" :disabled="busy" @click="openReject(r.id)">Ablehnen</button>
        </div>
        <div v-if="rejecting === r.id" style="margin-top:0.5rem;">
          <textarea v-model="rejectReason" rows="2" placeholder="Grund (optional)" maxlength="500"></textarea>
          <div class="actions">
            <button :disabled="busy" @click="decide(r.id, 'reject')">Ablehnung bestätigen</button>
            <button class="secondary" @click="rejecting = null">Abbrechen</button>
          </div>
        </div>
        <span v-if="msg[r.id]" class="status-badge" :class="msg[r.id].ok ? 'status-success' : 'status-danger'">{{ msg[r.id].text }}</span>
      </div>
    </div>
    <p v-if="error" class="status-badge status-danger">{{ error }}</p>
  </section>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useI18n } from "../lib/i18n";
import { apiFetch, ensureSession, getDemoUserId } from "../lib/api";

interface RequestRow { id: string; productId: string; productName: string; productSku: string; productUnit: string; quantity: number; justification?: string | null; status: string; requesterId: string; requesterName: string; createdAt: string; workspaceId: string; }
interface DemoUser { id: string; displayName: string; role: string; }

const requests = ref<RequestRow[]>([]);
const users = ref<DemoUser[]>([]);
const loading = ref(true);
const busy = ref(false);
const { t } = useI18n();
const error = ref("");
const rejecting = ref<string | null>(null);
const rejectReason = ref("");
const msg = ref<Record<string, { text: string; ok: boolean }>>({});
const pageMsg = ref<{ text: string; ok: boolean } | null>(null);

const selectedId = computed(() => getDemoUserId());
const selectedUser = computed(() => users.value.find((u) => u.id === selectedId.value));
const isManager = computed(() => selectedUser.value?.role === "Manager");
const pending = computed(() => requests.value.filter((r) => r.status === "Pending"));

onMounted(async () => {
  try {
    await ensureSession();
    await load();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "Laden fehlgeschlagen";
  } finally {
    loading.value = false;
  }
});

async function load() {
  const res = await apiFetch("/api/requests");
  if (!res.ok) throw new Error("Anfragen konnten nicht geladen werden");
  requests.value = await res.json();
  const ures = await apiFetch("/api/demo-users");
  if (ures.ok) users.value = await ures.json();
}

function openReject(id: string) { rejecting.value = id; rejectReason.value = ""; }

async function decide(id: string, action: "approve" | "reject") {
  busy.value = true;
  msg.value[id] = { text: "", ok: true };
  try {
    const body = action === "reject" ? { reason: rejectReason.value || null } : {};
    const res = await apiFetch(`/api/requests/${id}/${action}`, { method: "POST", body: JSON.stringify(body) });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      msg.value[id] = { text: (data && data.error) || `Fehler (HTTP ${res.status})`, ok: false };
      return;
    }
    msg.value[id] = { text: action === "approve" ? "Freigeben erfolgreich" : "Abgelehnt", ok: true };
    pageMsg.value = { text: action === "approve" ? "Freigeben erfolgreich" : "Abgelehnt", ok: true };
    rejecting.value = null;
    await load();
  } catch (e) {
    msg.value[id] = { text: e instanceof Error ? e.message : "Netzwerkfehler", ok: false };
  } finally {
    busy.value = false;
  }
}
</script>

<style scoped>
.secondary { background: var(--surface); color: var(--text); border: 1px solid var(--border); }
.status-warning { background: #fef3c7; color: #92400e; }
</style>
