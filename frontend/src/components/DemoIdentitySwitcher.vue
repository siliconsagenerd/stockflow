<template>
  <div class="card" style="margin-bottom:0.5rem;">
    <strong>Demo identity</strong>
    <span class="muted"> — simulated, not secure authentication.</span>
    <select v-model="selected" @change="onChange" style="margin-left:0.5rem;">
      <option v-for="u in users" :key="u.id" :value="u.id">{{ u.displayName }} ({{ u.role }})</option>
    </select>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { apiFetch, getDemoUserId, setDemoUserId } from "../lib/api";

interface DemoUser { id: string; displayName: string; role: string; }
const users = ref<DemoUser[]>([]);
const selected = ref("");

onMounted(async () => {
  try {
    const res = await apiFetch("/api/demo-users");
    if (res.ok) {
      users.value = await res.json();
      const stored = getDemoUserId();
      if (stored && users.value.some((u) => u.id === stored)) {
        selected.value = stored;
      } else if (users.value.length) {
        const emp = users.value.find((u) => u.role === "Employee") || users.value[0];
        selected.value = emp.id;
        setDemoUserId(emp.id);
      }
    }
  } catch { /* ignore */ }
});

function onChange() {
  setDemoUserId(selected.value);
}
</script>