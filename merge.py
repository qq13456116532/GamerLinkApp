import os

# 只导出的目录（相对路径）
INCLUDE_DIRS = {
    "Models",
    "Services",
    "ViewModels",
    "Views",
    "Platforms/Android"
}

# 只导出的文件（相对路径或文件名）
INCLUDE_FILES = {
    "Program.cs",
    "App.xml", "App.xml.cs",
    "AppShell.xaml", "AppShell.xaml.cs",
    "MainPage.xaml", "MainPage.xaml.cs",
    "MauiProgram.cs"
}

def export_to_txt(output_file="all_code.txt"):
    with open(output_file, "w", encoding="utf-8") as out:
        for root, dirs, files in os.walk("."):
            rel_root = os.path.relpath(root, ".").replace("\\", "/")

            for file in files:
                file_path = os.path.join(rel_root, file).replace("\\", "/")

                # ✅ 判断条件：目录匹配 或 文件匹配
                if (
                    any(file_path.startswith(d) for d in INCLUDE_DIRS) or
                    file_path in INCLUDE_FILES or
                    file in INCLUDE_FILES
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
    export_to_txt()
