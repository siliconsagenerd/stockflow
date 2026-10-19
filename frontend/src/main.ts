import { createApp } from "vue";
import { createRouter, createWebHistory } from "vue-router";
import App from "./App.vue";
import "./styles/main.css";

import Overview from "./views/Overview.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "overview", component: Overview },
    { path: "/requests", name: "requests", component: () => import("./views/Placeholder.vue") },
    { path: "/orders", name: "orders", component: () => import("./views/Placeholder.vue") },
    { path: "/inventory", name: "inventory", component: () => import("./views/Placeholder.vue") },
  ],
});

createApp(App).use(router).mount("#app");