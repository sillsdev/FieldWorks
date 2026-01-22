# A GUI utility to parse jenkins build results for repositories and commits
# and tag all the different repos with the same tag
# To be used when a release passes regression testing to record our base build
# and patch build contents
# First window takes the tag name and the text expected to be copied from the
# jenkins build results
# Example Expected Input:
# Revision: 65de9bfc2e656ec2b0f775d9385cc4fa04ed35c6
# Repository: https://github.com/sillsdev/FieldWorks
# refs/remotes/origin/release/9.3
# Revision: cd67d9d1a43d987229b60c5bb4d218fc4edabc69
# Repository: https://github.com/sillsdev/FwHelps
# refs/remotes/origin/develop
# Revision: 5b1f23f9654ad4620ec847db86ddb83c4bb4b278
# ect.
# After pasting it in clicking the button Parse Log and Tag
# will give an opportunity to select the local directories where
# each repository is cloned, this will be saved for future runs
# NOTE: This script was generated with AI assistance
import tkinter as tk
from tkinter import filedialog, messagebox, scrolledtext, ttk
import json
import os
import re
import subprocess

# Path to config file
CONFIG_FILE = "repo_paths.json"

# Regex to extract repo info from Jenkins log
REVISION_PATTERN = re.compile(
    r"Revision:\s+([a-f0-9]+).*?Repository:\s+(https://[^\s\n]+)",
    re.MULTILINE | re.DOTALL,
)


def load_repo_paths():
    if os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_repo_paths(paths):
    with open(CONFIG_FILE, "w", encoding="utf-8") as f:
        json.dump(paths, f, indent=2)


def tag_commit(repo_path, sha, tag_name):
    try:
        subprocess.run(["git", "fetch"], cwd=repo_path, check=True)
        subprocess.run(["git", "tag", tag_name, sha], cwd=repo_path, check=True)
        subprocess.run(
            ["git", "push", "origin", f"refs/tags/{tag_name}"],
            cwd=repo_path,
            check=True,
        )
        return True
    except subprocess.CalledProcessError as e:
        messagebox.showerror("Git Error", f"Failed to tag {repo_path}: {e}")
        return False


def parse_repos_from_log(log_text):
    matches = REVISION_PATTERN.findall(log_text)
    return [(sha, repo_url.strip()) for sha, repo_url in matches]


class GitTaggingApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Git Repository Tagging Tool")
        self.root.geometry("800x600")

        # Create main frame
        main_frame = ttk.Frame(root, padding="10")
        main_frame.grid(row=0, column=0, sticky=(tk.W, tk.E, tk.N, tk.S))

        # Configure grid weights
        root.columnconfigure(0, weight=1)
        root.rowconfigure(0, weight=1)
        main_frame.columnconfigure(1, weight=1)
        main_frame.rowconfigure(2, weight=1)

        # Tag name input
        ttk.Label(main_frame, text="Tag Name:").grid(
            row=0, column=0, sticky=tk.W, pady=(0, 5)
        )
        self.tag_entry = ttk.Entry(main_frame, width=30)
        self.tag_entry.grid(row=0, column=1, sticky=(tk.W, tk.E), pady=(0, 5))

        # Jenkins log input
        ttk.Label(main_frame, text="Jenkins Log Content:").grid(
            row=1, column=0, sticky=(tk.W, tk.N), pady=(5, 0)
        )

        # Text area for log content
        self.log_text = scrolledtext.ScrolledText(main_frame, height=20, width=80)
        self.log_text.grid(
            row=2, column=0, columnspan=2, sticky=(tk.W, tk.E, tk.N, tk.S), pady=(5, 10)
        )

        # Button frame
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=3, column=0, columnspan=2, pady=(5, 0))

        # Process button
        self.process_btn = ttk.Button(
            button_frame,
            text="Process Log & Tag Repositories",
            command=self.process_log,
        )
        self.process_btn.pack(side=tk.LEFT, padx=(0, 10))

        # Clear button
        self.clear_btn = ttk.Button(
            button_frame, text="Clear", command=self.clear_inputs
        )
        self.clear_btn.pack(side=tk.LEFT)

    def clear_inputs(self):
        self.tag_entry.delete(0, tk.END)
        self.log_text.delete(1.0, tk.END)

    def process_log(self):
        tag_name = self.tag_entry.get().strip()
        log_content = self.log_text.get(1.0, tk.END).strip()

        if not tag_name:
            messagebox.showerror("Error", "Please enter a tag name.")
            return

        if not log_content:
            messagebox.showerror("Error", "Please paste Jenkins log content.")
            return

        self.run_tagging(tag_name, log_content)

    def browse_repo(self, repo_url, label, repo_paths):
        path = filedialog.askdirectory(title=f"Select local path for {repo_url}")
        if path:
            repo_paths[repo_url] = path
            label.config(text=path)

    def run_tagging(self, tag_name, log_text):
        repo_paths = load_repo_paths()
        repos = parse_repos_from_log(log_text)

        if not repos:
            messagebox.showerror(
                "Error",
                "No repositories found in Jenkins log. Please check the log format.",
            )
            return

        # Create repository selection window
        repo_window = tk.Toplevel(self.root)
        repo_window.title("Select Local Repository Paths")
        repo_window.geometry("800x400")

        # Make window modal
        repo_window.transient(self.root)
        repo_window.grab_set()

        main_frame = ttk.Frame(repo_window, padding="10")
        main_frame.pack(fill=tk.BOTH, expand=True)

        # Create scrollable frame for repositories
        canvas = tk.Canvas(main_frame)
        scrollbar = ttk.Scrollbar(main_frame, orient="vertical", command=canvas.yview)
        scrollable_frame = ttk.Frame(canvas)

        scrollable_frame.bind(
            "<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all"))
        )

        canvas.create_window((0, 0), window=scrollable_frame, anchor="nw")
        canvas.configure(yscrollcommand=scrollbar.set)

        # Headers
        ttk.Label(
            scrollable_frame, text="Repository URL", font=("TkDefaultFont", 9, "bold")
        ).grid(row=0, column=0, sticky="w", padx=(0, 10), pady=(0, 10))
        ttk.Label(
            scrollable_frame, text="Local Path", font=("TkDefaultFont", 9, "bold")
        ).grid(row=0, column=1, sticky="w", padx=(0, 10), pady=(0, 10))
        ttk.Label(scrollable_frame, text="SHA", font=("TkDefaultFont", 9, "bold")).grid(
            row=0, column=2, sticky="w", padx=(0, 10), pady=(0, 10)
        )

        labels = {}
        for i, (sha, repo_url) in enumerate(repos, 1):
            # Repository URL (truncated for display)
            display_url = repo_url if len(repo_url) <= 50 else repo_url[:47] + "..."
            ttk.Label(scrollable_frame, text=display_url).grid(
                row=i, column=0, sticky="w", padx=(0, 10), pady=2
            )

            # Current path
            current_path = repo_paths.get(repo_url, "")
            path_text = current_path if current_path else "[Not set]"
            if len(path_text) > 40:
                path_text = "..." + path_text[-37:]

            label = ttk.Label(scrollable_frame, text=path_text, width=40)
            label.grid(row=i, column=1, sticky="w", padx=(0, 10), pady=2)
            labels[repo_url] = label

            # SHA (shortened)
            ttk.Label(scrollable_frame, text=sha[:8] + "...", font=("Courier", 8)).grid(
                row=i, column=2, sticky="w", padx=(0, 10), pady=2
            )

            # Browse button
            btn = ttk.Button(
                scrollable_frame,
                text="Browse",
                command=lambda u=repo_url, l=label: self.browse_repo(u, l, repo_paths),
            )
            btn.grid(row=i, column=3, padx=5, pady=2)

        canvas.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")

        # Button frame
        button_frame = ttk.Frame(repo_window)
        button_frame.pack(fill=tk.X, padx=10, pady=10)

        def on_submit():
            save_repo_paths(repo_paths)
            success_count = 0
            total_count = len(repos)

            for sha, repo_url in repos:
                local_path = repo_paths.get(repo_url)
                if not local_path or not os.path.isdir(local_path):
                    messagebox.showerror(
                        "Missing Path", f"No valid path selected for:\n{repo_url}"
                    )
                    return

                if tag_commit(local_path, sha, tag_name):
                    success_count += 1

            if success_count == total_count:
                messagebox.showinfo(
                    "Success",
                    f"Successfully tagged {success_count} repositories with tag '{tag_name}'.",
                )
            else:
                messagebox.showwarning(
                    "Partial Success",
                    f"Tagged {success_count} out of {total_count} repositories. Check error messages above.",
                )

            repo_window.destroy()

        def on_cancel():
            repo_window.destroy()

        ttk.Button(button_frame, text="Tag All Repositories", command=on_submit).pack(
            side=tk.RIGHT, padx=(5, 0)
        )
        ttk.Button(button_frame, text="Cancel", command=on_cancel).pack(side=tk.RIGHT)


def main():
    root = tk.Tk()
    GitTaggingApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
