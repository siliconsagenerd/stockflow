import { createRouter, createWebHistory } from "vue-router";
import Overview from "../views/Overview.vue";
import Requests from "../views/Requests.vue";
import Placeholder from "../views/Placeholder.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "overview", component: Overview },
    { path: "/requests", name: "requests", component: Requests },
    { path: "/orders", name: "orders", component: Placeholder },
    { path: "/inventory", name: "inventory", component: Placeholder },
  ],
});

export default router;