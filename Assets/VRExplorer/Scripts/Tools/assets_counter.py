import os
import re
import csv

root_path = None
def get_unity_version(project_path):
    version_file = os.path.join(project_path, 'ProjectSettings', 'ProjectVersion.txt')
    if os.path.exists(version_file):
        with open(version_file, 'r') as f:
            version = f.readline().strip()
            return version.split(' ')[1]
    return "Unknown"

def count_csharp_scripts(project_path):
    script_count = 0
    total_lines = 0

    def read_file_lines(file_path):
        for encoding in ('utf-8', 'gbk', 'latin1'):
            try:
                with open(file_path, 'r', encoding=encoding) as f:
                    return f.readlines()
            except Exception:
                continue
        print(f"无法读取文件 {file_path}，尝试了多种编码失败。")
        return []

    for root, dirs, files in os.walk(os.path.join(project_path, 'Assets')):
        for file in files:
            if file.endswith('.cs'):
                script_count += 1
                file_path = os.path.join(root, file)
                lines = read_file_lines(file_path)
                for line in lines:
                    if line.strip():
                        total_lines += 1

    return script_count, total_lines


def count_files_in_directory(path, include_meta=False):
    count = 0
    for root, _, files in os.walk(path):
        for file in files:
            if include_meta or not file.endswith('.meta'):
                count += 1
    return count

def count_files_by_extension(root_path, extension):
    count = 0
    for root, _, files in os.walk(root_path):
        for file in files:
            if file.lower().endswith(extension.lower()):
                count += 1
    return count

def get_scenes_and_gameobjects(project_path):
    scene_files = []
    gameobject_count = 0
    for root, _, files in os.walk(os.path.join(project_path, 'Assets')):
        for file in files:
            if file.endswith('.unity'):
                scene_files.append(os.path.join(root, file))

    for scene in scene_files:
        try:
            with open(scene, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                gameobject_count += len(re.findall(r'm_GameObject', content))
        except Exception as e:
            print(f"Failed to parse {scene}: {e}")

    return len(scene_files), gameobject_count
def get_first_subdirectory(A, B):
    # 去掉前缀路径 B
    relative_path = os.path.relpath(A, B)
    # 拆分路径并获取第一个部分
    first_subdir = relative_path.split(os.sep)[0]
    return first_subdir
def analyze_project(project_path):
    print(f"分析项目: {project_path}")
    version = get_unity_version(project_path)
    cs_count, cs_lines = count_csharp_scripts(project_path)
    total_files = count_files_in_directory(os.path.join(project_path, 'Assets'), include_meta=False)
    prefab_count = count_files_by_extension(os.path.join(project_path, 'Assets'), ".prefab")
    scene_count, gameobject_count = get_scenes_and_gameobjects(project_path)

    return {
        'Project': get_first_subdirectory(project_path, root_path),
        'UnityVersion': version,
        'CSharpScripts': cs_count,
        'CSharpLines': cs_lines,
        'NonMetaFiles': total_files,
        'Prefabs': prefab_count,
        'Scenes': scene_count,
        'GameObjects': gameobject_count
    }

def find_unity_projects(root_path):
    unity_projects = []
    for dirpath, dirnames, filenames in os.walk(root_path):
        # 判定条件：目录下有Assets文件夹和ProjectSettings文件夹
        if 'Assets' in dirnames and 'ProjectSettings' in dirnames:
            unity_projects.append(dirpath)
            # 为避免重复，阻止再往下递归子目录，因为子目录不可能是独立项目了
            dirnames.clear()
    return unity_projects

def main(root_path):
    unity_projects = find_unity_projects(root_path)

    results = []
    for project_path in unity_projects:
        result = analyze_project(project_path)
        results.append(result)

    csv_file = os.path.join(root_path, 'unity_projects_summary.csv')
    with open(csv_file, 'w', newline='', encoding='utf-8') as f:
        fieldnames = ['Project', 'UnityVersion', 'CSharpScripts', 'CSharpLines', 'NonMetaFiles', 'Prefabs', 'Scenes', 'GameObjects']
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for row in results:
            writer.writerow(row)

    print(f"统计完成，结果已保存至 {csv_file}")

if __name__ == '__main__':
    root_path = input("请输入包含多个Unity项目的根目录路径: ")
    main(root_path)

