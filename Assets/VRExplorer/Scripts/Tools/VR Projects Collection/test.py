import os
import re

def is_valid_char(char):
    """检查字符是否合法（a-z, A-Z, 0-9, _, +, -, ., ,, =）"""
    return char.isalnum() or char in {'_', '+', '-', '.', ',', '='}

def sanitize_filename(filename):
    """替换非法字符为下划线 _"""
    new_name = []
    for char in filename:
        if is_valid_char(char):
            new_name.append(char)
        else:
            new_name.append('_')  # 替换非法字符
    return ''.join(new_name)

def rename_files_in_directory(directory):
    """遍历文件夹并重命名文件"""
    for root, dirs, files in os.walk(directory):
        for name in files + dirs:
            old_path = os.path.join(root, name)
            new_name = sanitize_filename(name)
            new_path = os.path.join(root, new_name)

            if new_name != name:  # 如果文件名有变化
                try:
                    os.rename(old_path, new_path)
                    print(f"✅ 重命名: {name} → {new_name}")
                except Exception as e:
                    print(f"❌ 重命名失败: {name} (错误: {e})")

if __name__ == "__main__":
    target_dir = input("请输入要处理的文件夹路径: ").strip()
    if os.path.isdir(target_dir):
        print(f"🔍 正在处理文件夹: {target_dir}")
        rename_files_in_directory(target_dir)
        print("🎉 文件名清理完成！")
    else:
        print("❌ 错误: 文件夹路径无效！")