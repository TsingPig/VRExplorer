import json
import os
import shutil
import argparse
from pathlib import Path

def find_file_in_subdirectories(root_path, filename):
    """在子目录中递归查找文件"""
    for file_path in root_path.rglob(filename):
        return file_path
    return None

def move_cs_files_enhanced(json_file_path, total_path, core_path):
    """
    增强版的.cs文件搬迁，支持自动查找文件
    """
    # 验证路径
    total_path = Path(total_path).resolve()
    core_path = Path(core_path).resolve()
    
    if not total_path.exists():
        print(f"错误: 总路径不存在: {total_path}")
        return False
    
    if not core_path.exists():
        print(f"错误: Core路径不存在: {core_path}")
        return False
    
    # 读取JSON文件
    try:
        with open(json_file_path, 'r', encoding='utf-8') as f:
            cs_files = json.load(f)
        print(f"从JSON文件读取到 {len(cs_files)} 个.cs文件")
    except Exception as e:
        print(f"错误: 无法读取JSON文件: {e}")
        return False
    
    print(f"总路径: {total_path}")
    print(f"Core路径: {core_path}")
    print("-" * 60)
    
    moved_count = 0
    skipped_count = 0
    error_count = 0
    found_files = []
    
    # 第一步：查找所有文件的实际位置
    print("正在查找文件...")
    for i, cs_file in enumerate(cs_files, 1):
        file_path = Path(cs_file)
        
        # 尝试直接路径
        source_file = total_path / cs_file
        if source_file.exists():
            found_files.append((str(cs_file), source_file))
            print(f"[{i}/{len(cs_files)}] ✓ 找到: {cs_file}")
            continue
        
        # 如果只有文件名，在子目录中搜索
        if '/' not in cs_file and '\\' not in cs_file:
            found_path = find_file_in_subdirectories(total_path, cs_file)
            if found_path:
                relative_path = found_path.relative_to(total_path)
                found_files.append((str(relative_path), found_path))
                print(f"[{i}/{len(cs_files)}] ✓ 找到(搜索): {cs_file} -> {relative_path}")
            else:
                print(f"[{i}/{len(cs_files)}] ✗ 未找到: {cs_file}")
                error_count += 1
        else:
            print(f"[{i}/{len(cs_files)}] ✗ 未找到: {cs_file}")
            error_count += 1
    
    if not found_files:
        print("没有找到任何文件，无法继续")
        return False
    
    print(f"\n找到 {len(found_files)} 个文件，开始搬迁...")
    print("-" * 60)
    
    # 第二步：搬迁文件
    for i, (relative_path, source_file) in enumerate(found_files, 1):
        target_file = core_path / relative_path
        
        # 检查目标文件是否已存在
        if target_file.exists():
            print(f"[{i}/{len(found_files)}] 跳过: 文件已存在 - {relative_path}")
            skipped_count += 1
            continue
        
        # 创建目标目录
        target_file.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            # 移动文件
            shutil.move(str(source_file), str(target_file))
            print(f"[{i}/{len(found_files)}] 移动: {relative_path}")
            moved_count += 1
            
        except Exception as e:
            print(f"[{i}/{len(found_files)}] 错误: 移动文件失败 {relative_path}: {e}")
            error_count += 1
    
    print("-" * 60)
    print(f"处理完成:")
    print(f"成功移动: {moved_count} 个文件")
    print(f"跳过(已存在): {skipped_count} 个文件")
    print(f"错误: {error_count} 个文件")
    
    # 生成修正后的JSON文件
    if found_files:
        fixed_files = [rel_path for rel_path, _ in found_files]
        fixed_json = json_file_path.replace('.json', '_fixed.json')
        with open(fixed_json, 'w', encoding='utf-8') as f:
            json.dump(fixed_files, f, indent=2, ensure_ascii=False)
        print(f"\n已生成修正后的JSON文件: {fixed_json}")
    
    return error_count == 0

def main():
    parser = argparse.ArgumentParser(description='增强版.cs文件搬迁工具')
    parser.add_argument('total_path', help='总路径')
    parser.add_argument('core_path', help='Core路径')
    parser.add_argument('json_file', help='JSON文件路径')
    
    args = parser.parse_args()
    
    success = move_cs_files_enhanced(args.json_file, args.total_path, args.core_path)
    return 0 if success else 1

if __name__ == "__main__":
    # 交互式界面
    if len(os.sys.argv) == 1:
        print("增强版CS文件搬迁工具")
        print("=" * 50)
        
        json_file = input("请输入JSON文件路径: ").strip()
        total_path = input("请输入总路径: ").strip()
        core_path = input("请输入Core路径: ").strip()
        
        if not all([json_file, total_path, core_path]):
            print("所有参数都必须提供")
            exit(1)
        
        # 先检查JSON文件格式
        from pathlib import Path
        total_path_obj = Path(total_path).resolve()
        
        print("\n检查JSON文件格式...")
        with open(json_file, 'r', encoding='utf-8') as f:
            cs_files = json.load(f)
        
        print(f"JSON中包含 {len(cs_files)} 个文件")
        print("前5个文件示例:")
        for i, file_path in enumerate(cs_files[:5]):
            print(f"  {i+1}: {file_path}")
        
        confirm = input("\n是否继续? (y/n): ").strip().lower()
        if confirm == 'y':
            success = move_cs_files_enhanced(json_file, total_path, core_path)
            exit(0 if success else 1)
        else:
            print("操作已取消")
            exit(0)
    else:
        exit(main())