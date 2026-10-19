<template>
  <div class="card" role="status" aria-live="polite">
    <strong>API / database:</strong>
    <span v-if="loading" class="status-badge status-neutral">checking…</span>
    <span v-else-if="ok" class="status-badge status-success">connected</span>
    <span v-else class="status-badge status-danger">unreachable</span>
    <span v-if="error" class="muted" style="margin-left:0.5rem;">{{ error }}</span>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";

const loading = ref(true);
const ok = ref(false);
const error = ref("");

onMounted(async () => {
  try {
    const res = await fetch("/health");
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    ok.value = data?.database === "connected";
    if (!ok.value) error.value = data?.database ?? "unknown";
  } catch (e) {
    ok.value = false;
    error.value = e instanceof Error ? e.message : "network error";
  } finally {
    loading.value = false;
  }
});
</script>