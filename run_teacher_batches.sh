#!/usr/bin/env bash
set -e

echo "Running Sakkirina vs MCTSBot..."
dotnet run --project GameRunner -- \
  --runs 200 \
  --threads 4 \
  --enable-logs NONE \
  --log logs/teacher_sakk_vs_mcts_200.jsonl \
  SakkirinaSolo MCTSBot

echo "Running Sakkirina vs RandomBot..."
dotnet run --project GameRunner -- \
  --runs 200 \
  --threads 4 \
  --enable-logs NONE \
  --log logs/teacher_sakk_vs_random_200.jsonl \
  SakkirinaSolo RandomBot

echo "Running Sakkirina vs Sakkirina..."
dotnet run --project GameRunner -- \
  --runs 200 \
  --threads 4 \
  --enable-logs NONE \
  --log logs/teacher_sakk_vs_sakk_200.jsonl \
  SakkirinaSolo SakkirinaSolo

echo "Running Sakkirina vs MaxPrestigeBot..."
dotnet run --project GameRunner -- \
  --runs 200 \
  --threads 4 \
  --enable-logs NONE \
  --log logs/teacher_sakk_vs_maxprestige_200.jsonl \
  SakkirinaSolo MaxPrestigeBot

echo "All batches finished."
