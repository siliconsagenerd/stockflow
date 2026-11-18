# StockFlow — Production Runbook

## Deployed URLs
- **Frontend**: https://stockflow-frontend-mauve.vercel.app
- **API (HTTPS)**: https://stockflowap1.duckdns.org
- **Health check**: https://stockflowap1.duckdns.org/health

## Deployment Details
- **Current deployed HEAD**: 6316a4ea5af75294121183bdc44fa8652fb27c63
- **API image built from**: bdb999c (docs-only changes since; API image unchanged)
- **Image tag**: stockflow-api:latest
- **Deployment date**: 2026-10-20
- **Frontend**: Vercel (hobby plan)
- **API host**: Oracle Cloud (eu-frankfurt-1, VM.Standard.A1.Flex)
- **Database**: Supabase Postgres (eu-central-1; project ref redacted — see private ops notes)

## Health Check
```bash
curl -s https://stockflowap1.duckdns.org/health
# Expected: {"status":"Healthy","process":"ok","database":"connected"}
```

## View Logs

### Container logs
```bash
ssh -i <private-key> ubuntu@<oci-host> "sudo docker logs stockflow-api --tail 50"
```

### Caddy logs
```bash
ssh -i <private-key> ubuntu@<oci-host> "sudo journalctl -u caddy --no-pager -n 50"
```

### Follow logs in real time
```bash
ssh -i <private-key> ubuntu@<oci-host> "sudo docker logs -f stockflow-api"
```

## Redeploy Same Version
```bash
# 1. Sync source from oldmac (OCI dir is NOT a git repo — git pull fails there)
cd ~/projects/stockflow && tar czf - backend Dockerfile | \
  ssh -i <private-key> ubuntu@<oci-host> "cd /home/ubuntu/stockflow && tar xzf -"

# 2. On OCI instance:
ssh -i <private-key> ubuntu@<oci-host>
cd /home/ubuntu/stockflow && sudo docker build -q -t stockflow-api:latest .
sudo docker stop stockflow-api && sudo docker rm stockflow-api
sudo docker run -d --name stockflow-api --restart unless-stopped \
  --env-file /home/ubuntu/stockflow.env \
  -p 127.0.0.1:8080:8080 stockflow-api:latest
```

## Rollback

### Frontend (Vercel)
```bash
# Via CLI:
vercel rollback
# Or via Vercel dashboard: Deployments → select previous → Promote to Production
```

### API (Oracle)
```bash
# Rebuild from a specific commit:
cd /home/ubuntu/stockflow && git checkout <commit> && sudo docker build -t stockflow-api:rollback .
sudo docker stop stockflow-api && sudo docker rm stockflow-api
sudo docker run -d --name stockflow-api --restart unless-stopped \
  --env-file /home/ubuntu/stockflow.env \
  -p 127.0.0.1:8080:8080 stockflow-api:rollback
```

## Supabase Backup/Restore
- **Free tier retention**: 7 days (daily backups)
- **Manual backup**: `supabase db dump --project-ref <project-ref>`
- **Restore**: Contact Supabase support or use `pg_restore` with a dump file
- **Note**: Do not run destructive resets against production

## Oracle Cloud Expiry & Migration Checklist
- **Instance**: VM.Standard.A1.Flex in eu-frankfurt-1
- **Credit**: Available credit, short-lived deployment
- **Migration steps** (estimated 30-60 min):
  1. Provision new host (AWS EC2, oldmac, or other)
  2. Install Docker + Caddy/Nginx
  3. Copy Dockerfile and build: `docker build -t stockflow-api .`
  4. Copy env file (same variables, no code changes)
  5. Run container: `docker run -d --env-file stockflow.env -p 127.0.0.1:8080:8080 stockflow-api`
  6. Configure reverse proxy with HTTPS (Caddy auto-HTTPS or Let's Encrypt)
  7. Update DNS (DuckDNS or new domain) to new host IP
  8. Update Vercel rewrites if domain changes
  9. Verify health check
  10. Database stays Supabase (no migration needed)
  11. No application code changes required

## Load Test Result

- 8 concurrent users, each completing request -> approval -> receipt (6 + 4)
- 8/8 workspaces, requests, approvals, and purchase orders succeeded
- Inventory correct (10 units) for 5 of 8 concurrent users
- 3 serialization conflicts (HTTP 409) on concurrent receipt submissions
- Concurrent receipt submissions may return retryable 409 responses
- The demo is verified for sequential use
- Do not claim the concurrent load test fully passed

## Connection Mode
- **Mode**: Supavisor transaction pooler (port 6543)
- **Host**: aws-1-eu-central-1.pooler.supabase.com
- **Pooling**: Disabled in Npgsql (Pooling=false) due to intermittent connection issues
- **Practical concurrent-user limit**: ~5-10 (each request opens a new connection; pooler handles multiplexing)
- **Note**: For higher concurrency, re-enable Npgsql pooling (Pooling=true, Max Pool Size=10) and test

## Known Limitations
- Anonymous sessions (possession-based, no authentication)
- Demo identities are simulated (not enterprise authorization)
- Zero opening stock (stock enters only via goods receipts)
- No real SAP/ERP integration
- No multi-warehouse, no transfers
- MSB3277 warning (EF Core/Npgsql version skew, not suppressed)
- Supabase free tier (7-day backup retention, 500MB limit)
