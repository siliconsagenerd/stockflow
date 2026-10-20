# Demo Walkthrough — StockFlow (target, Phase 6)

Two-minute script (to be finalized after features land):

1. Open app; show API/database connected.
2. Create request for 10 replacement filters (Employee).
3. Switch to Manager; approve request.
4. Show purchase order created (Ordered).
5. Switch to Warehouse; receive 6 (partial).
6. Show stock +6, order Partially received, 4 outstanding.
7. Receive remaining 4.
8. Show stock +4, order Completed.
9. Show history timeline.
10. Demonstrate one rejected invalid action (e.g., over-receipt or self-approval).
## Phase 3 walkthrough
1. Open app, create session (automatic).
2. Switch to Anna Beispiel (Employee). Create a request (product + quantity).
3. Switch to Markus Beispiel (Manager). Go to Approvals — see the Pending request.
4. Click Approve. Request disappears from Approvals.
5. Go to Purchase Orders — one Open order appears with reference, product, supplier, qty.
6. Reload page — order persists.
7. Create another request as Employee, switch to Manager, click Reject with a reason — request marked Rejected, no order created.
8. Non-Manager identities see Switch to a Manager identity guidance; approve/reject buttons hidden.

## Phase 4 walkthrough
1. Employee creates request for 10 units, Manager approves → PO (Open, 10 ordered).
2. Switch to Werner (Warehouse). Open PO detail. Receive 6 → PartiallyReceived, stock +6.
3. Receive remaining 4 → Completed, stock +10.
4. Check Inventory → 10 pcs, movement history shows +6 and +4.
5. Reload → state persists.
