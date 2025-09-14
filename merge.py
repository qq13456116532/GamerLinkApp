import os

# 需要忽略的目录
EXCLUDE_DIRS = {"Properties", "bin", "obj", ".vs", ".git", "node_modules", "Platforms", "Resources"}

# 需要忽略的文件
EXCLUDE_FILES = {
    ".gitignore","AssemblyInfo.cs", "merge.py","README.md",
    "GamerLinkApp.csproj","GamerLinkApp.csproj.user","GamerLinkApp.sln"
}

def export_cs_to_txt(output_file="all_code.txt"):
    with open(output_file, "w", encoding="utf-8") as out:
        for root, dirs, files in os.walk("."):
            # 修改 dirs 原地过滤，跳过不需要的文件夹
            dirs[:] = [d for d in dirs if d not in EXCLUDE_DIRS]

            for file in files:
                if file.endswith(".cs") and file not in EXCLUDE_FILES:
                    file_path = os.path.join(root, file)
                    rel_path = os.path.relpath(file_path, ".")  # 相对路径

                    try:
                        with open(file_path, "r", encoding="utf-8") as f:
                            content = f.read()
                    except UnicodeDecodeError:
                        with open(file_path, "r", encoding="latin-1") as f:
                            content = f.read()

                    out.write(f"{rel_path}:\n")
                    out.write(f"【{content}】\n\n")

    print(f"✅ 已导出到 {output_file}")

if __name__ == "__main__":
    export_cs_to_txt()
