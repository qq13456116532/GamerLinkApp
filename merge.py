import os

# 只导出的目录（相对路径）
INCLUDE_DIRS = {
    "Models",
    "Services",
    "ViewModels","Views"
}

# 只导出的文件（相对路径或文件名）
INCLUDE_FILES = {
    "Program.cs",
    "App.xml","App.xml.cs","AppShell.xaml","AppShell.xaml.cs","MainPage.xaml","MainPage.xaml.cs","MauiProgram.cs"
}

def export_cs_to_txt(output_file="all_code.txt"):
    with open(output_file, "w", encoding="utf-8") as out:
        for root, dirs, files in os.walk("."):
            rel_root = os.path.relpath(root, ".")
            
            # 如果当前目录不在指定目录里，直接跳过
            if not any(rel_root.startswith(d) for d in INCLUDE_DIRS) and rel_root != ".":
                continue

            for file in files:
                file_path = os.path.join(rel_root, file).replace("\\", "/")

                # 判断文件是否在白名单里
                if file.endswith(".cs") and (
                    file_path in INCLUDE_FILES or file in INCLUDE_FILES or
                    any(file_path.startswith(d) for d in INCLUDE_DIRS)
                ):
                    try:
                        with open(file_path, "r", encoding="utf-8") as f:
                            content = f.read()
                    except UnicodeDecodeError:
                        with open(file_path, "r", encoding="latin-1") as f:
                            content = f.read()

                    out.write(f"{file_path}:\n")
                    out.write(f"【{content}】\n\n")

    print(f"✅ 已导出到 {output_file}")

if __name__ == "__main__":
    export_cs_to_txt()
