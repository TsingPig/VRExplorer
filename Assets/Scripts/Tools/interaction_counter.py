import os
import json
import re
import csv

# 读取规则
with open("rules.json", "r", encoding="utf-8") as f:
    RULES = json.load(f)

def scan_file(file_path):
    """读取文件并返回里面出现的匹配关键词"""
    hits = {rule: 0 for rule in RULES.keys()}
    try:
        text = open(file_path, "r", encoding="utf-8", errors="ignore").read()
    except Exception:
        return hits

    for rule, cfg in RULES.items():
        # 检查 GameObject 名称（m_Name: Button）
        for go in cfg.get("gameObjects", []):
            if re.search(rf"m_Name:\s*{go}\b", text):
                hits[rule] += 1

        # 检查脚本类名（MonoBehaviour 里 script 或 C# 类名）
        for script in cfg.get("scripts", []):
            if script in text:
                hits[rule] += 1

    return hits

def scan_project(project_path):
    """扫描单个 Unity 项目"""
    project_stats = {rule: 0 for rule in RULES.keys()}
    assets_path = os.path.join(project_path, "Assets")

    for root, _, files in os.walk(assets_path):
        for file in files:
            if file.endswith((".unity", ".prefab", ".asset", ".cs")):
                file_path = os.path.join(root, file)
                hits = scan_file(file_path)
                for rule, count in hits.items():
                    project_stats[rule] += count

    return project_stats

def main(projects_root, output_csv="interaction_stats.csv"):
    results = []

    for project in os.listdir(projects_root):
        project_path = os.path.join(projects_root, project)
        if not os.path.isdir(project_path):
            continue

        stats = scan_project(project_path)
        stats["Project"] = project
        results.append(stats)

    # 写出 CSV
    fieldnames = ["Project"] + list(RULES.keys())
    with open(output_csv, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for row in results:
            writer.writerow(row)

    print(f"统计完成，结果已保存到 {output_csv}")

if __name__ == "__main__":
    main("D:\--UnityProject\VR\VRExplorer_projects_dataset")  # 这里换成你 100+ 项目的父目录
