#!/usr/bin/env bash
# Ngarkon EC2 për të rritur metrikat CloudWatch (CPU, Network, Disk).
#
# --- Komanda (ekzekuto nga laptopi) ---
#
# 1) Kopjo skriptën nga MAC (terminal i ri, jo brenda SSH):
#    scp -i ~/Downloads/cloudguard.pem \
#      /Users/ritech/cloudguard-ai/scripts/load-ec2-metrics.sh \
#      ubuntu@54.196.143.242:~/
#
# 2) Lidhu me EC2:
#    ssh -i ~/Downloads/cloudguard.pem ubuntu@54.196.143.242
#
# 3) Në EC2, ekzekuto:
#    sed -i 's/\r$//' load-ec2-metrics.sh   # vetëm nëse merr bash\r error
#    chmod +x load-ec2-metrics.sh
#    ./load-ec2-metrics.sh           # të gjitha vCPU-të, 1 orë
#    ./load-ec2-metrics.sh 8 1800    # 8 CPU, 30 min
#
# 4) Pas 2-5 min në CloudGuard UI: Import Cloud → Last 1 hour → Import
#
# Përdorim: ./load-ec2-metrics.sh [cpu_cores] [seconds]

set -euo pipefail

default_cpu_cores() {
  if command -v nproc &>/dev/null; then
    nproc
  else
    echo 4
  fi
}

CPU_CORES="${1:-$(default_cpu_cores)}"
DURATION="${2:-3600}"

echo "=== CloudGuard load test ==="
echo "CPU cores: $CPU_CORES | Duration: ${DURATION}s"
echo "Pas skriptës, bëj Import Cloud në CloudGuard (Last 1 hour ose 15 min)"
echo ""

cleanup() {
  echo ""
  echo "Duke ndalur proceset..."
  jobs -p 2>/dev/null | xargs kill 2>/dev/null || true
  rm -f /tmp/cloudguard_loadtest 2>/dev/null || true
  echo "Përfundoi."
}
trap cleanup EXIT INT TERM

echo "[1/3] CPU stress ($CPU_CORES cores)..."
if ! command -v stress &>/dev/null; then
  echo "Installing stress..."
  sudo apt-get update -qq && sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq stress || true
fi
if command -v stress &>/dev/null; then
  stress --cpu "$CPU_CORES" --timeout "$DURATION" &
else
  for ((i = 0; i < CPU_CORES; i++)); do
    timeout "$DURATION" bash -c 'while true; do :; done' &
  done
fi

echo "[2/3] Network load..."
for _ in {1..5}; do
  timeout "$DURATION" bash -c '
    while true; do
      curl -fsS -o /dev/null --max-time 30 \
        https://speed.hetzner.de/10MB.bin 2>/dev/null || sleep 2
    done
  ' &
done

echo "[3/4] Disk write/read loop..."
(
  end=$((SECONDS + DURATION))
  while [ "$SECONDS" -lt "$end" ]; do
    dd if=/dev/zero of=/tmp/cloudguard_loadtest bs=1M count=50 2>/dev/null || true
    dd if=/tmp/cloudguard_loadtest of=/dev/null bs=1M 2>/dev/null || true
    sleep 2
  done
  rm -f /tmp/cloudguard_loadtest 2>/dev/null || true
) &

echo "[4/4] Memory stress..."
if command -v stress &>/dev/null; then
  # ~70% of total RAM in MB (stress does not accept % suffix)
  vm_mb=$(awk '/MemTotal/ {printf "%d", $2*0.7/1024}' /proc/meminfo)
  stress --vm 1 --vm-bytes "${vm_mb}M" --timeout "$DURATION" &
else
  timeout "$DURATION" bash -c '
    chunk="$(head -c 104857600 /dev/zero | tr "\0" "x")"
    while true; do :; done
  ' &
fi

echo ""
echo "Ngarkesa aktive për ${DURATION}s (1 orë). Prit 2-5 min, pastaj Import Cloud → Last 1 hour."
wait
