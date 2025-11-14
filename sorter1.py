import os
import re
import json
import customtkinter as ctk
from tkinter import filedialog, messagebox
from PIL import Image
import threading

# --- Constants for a Compact and Professional UI Design ---
COMPACT_ROW_HEIGHT = 36 # Reduced row height to fit more content
COLOR_BACKGROUND = "#1c1c1c"
COLOR_FRAME = "#2a2a2a"
COLOR_HEADER_BG = "#283593"
COLOR_ROW_SEPARATOR = "#3a3a3a"
COLOR_TEXT_HEADER = "#ffffff"
COLOR_TEXT_DEFAULT = "#dcdcdc"
COLOR_TEXT_COUNT = "#f1c40f"
COLOR_TEXT_ACTIVE = "#2ecc71"
COLOR_TEXT_INACTIVE = "#e74c3c"
FONT_BOLD = ("Roboto", 13, "bold")
FONT_NORMAL = ("Roboto", 12)

# --- Main Application Class ---
class App(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("Log Sorter")
        self.geometry("1200x800")
        
        # Start maximized to provide maximum vertical space from the beginning
        self.after(100, lambda: self.state('zoomed'))

        ctk.set_appearance_mode("Dark")
        
        self.ICON_DIR = "skin_icons"
        self.image_cache = {}
        self.selected_folders = []
        
        self.vbuck_range_names = {"1-50", "51-100", "100-200", "200-500", "500-2000", "2000-5000", "5000+"}
        self.skin_range_names = {"6-20", "20-50", "50-100", "100-150", "150-200", "200+"}
        
        self.placeholder_icon = self.create_placeholder_icon()
        self.vbucks_icon = self.load_special_icon("vbucks_icon.png")
        self.skin_range_icon = self.load_special_icon("skin_range_icon.png")
        
        if not os.path.exists(self.ICON_DIR):
            print(f"Warning: Directory '{self.ICON_DIR}' not found. Run 'download_icons.py' first.")

        self.create_widgets()

    def create_placeholder_icon(self):
        try:
            img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
            from PIL import ImageDraw
            draw = ImageDraw.Draw(img)
            draw.polygon([(32, 2), (40, 20), (62, 22), (46, 38), (50, 60), (32, 48), (14, 60), (18, 38), (2, 22), (24, 20)], fill='white')
            return ctk.CTkImage(light_image=img, dark_image=img, size=(30, 30))
        except ImportError: return None
            
    def load_special_icon(self, filename):
        filepath = os.path.join(self.ICON_DIR, filename)
        if os.path.exists(filepath):
            try:
                pil_image = Image.open(filepath)
                return ctk.CTkImage(light_image=pil_image, dark_image=pil_image, size=(30, 30))
            except Exception as e: print(f"Error loading special icon {filename}: {e}")
        return self.placeholder_icon

    def sanitize_filename(self, name):
        name = name.lower()
        name = re.sub(r'\s+', '_', name)
        name = re.sub(r'[^\w\-_\.]', '', name)
        return f"{name}.png"
        
    def get_icon_for_category(self, category_name):
        if category_name in self.vbuck_range_names: return self.vbucks_icon
        if category_name in self.skin_range_names: return self.skin_range_icon

        filename = self.sanitize_filename(category_name)
        if filename in self.image_cache: return self.image_cache[filename]
            
        filepath = os.path.join(self.ICON_DIR, filename)
        if os.path.exists(filepath):
            try:
                pil_image = Image.open(filepath)
                ctk_image = ctk.CTkImage(light_image=pil_image, dark_image=pil_image, size=(30, 30))
                self.image_cache[filename] = ctk_image
                return ctk_image
            except Exception as e: print(f"Error loading image {filepath}: {e}")
        return self.placeholder_icon

    def create_widgets(self):
        self.configure(fg_color=COLOR_BACKGROUND)
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(2, weight=1)

        main_header = ctk.CTkFrame(self, fg_color=COLOR_HEADER_BG, corner_radius=0)
        main_header.grid(row=0, column=0, sticky="ew")
        main_header.grid_columnconfigure(1, weight=1)
        
        title_label = ctk.CTkLabel(main_header, text="Log Summary", font=("Roboto", 20, "bold"), text_color=COLOR_TEXT_HEADER)
        title_label.grid(row=0, column=0, padx=20, pady=10, sticky="w")
        
        self.active_label = ctk.CTkLabel(main_header, text="Total Active: 0", font=FONT_BOLD, text_color=COLOR_TEXT_ACTIVE)
        self.active_label.grid(row=0, column=1, padx=(0,10), sticky="e")

        self.inactive_label = ctk.CTkLabel(main_header, text="Total Inactive: 0", font=FONT_BOLD, text_color=COLOR_TEXT_INACTIVE)
        self.inactive_label.grid(row=0, column=2, padx=(0,20), sticky="e")
        
        controls_frame = ctk.CTkFrame(self, fg_color="transparent")
        controls_frame.grid(row=1, column=0, sticky="ew", padx=10, pady=10)
        controls_frame.grid_columnconfigure(0, weight=1)

        self.select_button = ctk.CTkButton(controls_frame, text="Select Folder(s) and Start Sorting", command=self.start_folder_selection_loop)
        self.select_button.pack(pady=5)
        
        self.status_bar = ctk.CTkLabel(controls_frame, text="Please select a folder to begin.", font=FONT_NORMAL, text_color=COLOR_TEXT_DEFAULT)
        self.status_bar.pack()

        self.results_frame = ctk.CTkScrollableFrame(self, fg_color=COLOR_FRAME, corner_radius=10, border_width=2, border_color=COLOR_ROW_SEPARATOR)
        self.results_frame.grid(row=2, column=0, sticky="nsew", padx=10, pady=(0, 10))
        
        self.create_table_header()

    def create_table_header(self):
        self.header = ctk.CTkFrame(self.results_frame, fg_color="transparent")
        self.header.pack(fill="x", pady=5, padx=10)
        self.header.grid_columnconfigure(0, weight=1); self.header.grid_columnconfigure(1, weight=2) 
        self.header.grid_columnconfigure(2, weight=12); self.header.grid_columnconfigure(3, weight=3)
        self.header.grid_columnconfigure(4, weight=3)
        ctk.CTkLabel(self.header, text="#", font=FONT_BOLD, text_color=COLOR_TEXT_DEFAULT).grid(row=0, column=0)
        ctk.CTkLabel(self.header, text="Count", font=FONT_BOLD, text_color=COLOR_TEXT_DEFAULT).grid(row=0, column=1)
        ctk.CTkLabel(self.header, text="Category / Priority", anchor="w", font=FONT_BOLD, text_color=COLOR_TEXT_DEFAULT).grid(row=0, column=2, sticky="w", padx=40)
        ctk.CTkLabel(self.header, text="Active", font=FONT_BOLD, text_color=COLOR_TEXT_DEFAULT).grid(row=0, column=3)
        ctk.CTkLabel(self.header, text="Inactive", font=FONT_BOLD, text_color=COLOR_TEXT_DEFAULT).grid(row=0, column=4)
        ctk.CTkFrame(self.results_frame, height=2, fg_color=COLOR_ROW_SEPARATOR).pack(fill="x", padx=5)

    def clear_table(self):
        for widget in self.results_frame.winfo_children():
            if widget != self.header: widget.destroy()
    
    def add_table_row(self, index, count, category, active, inactive):
        # Using a compact layout with less vertical padding
        row_frame = ctk.CTkFrame(self.results_frame, fg_color="transparent", height=COMPACT_ROW_HEIGHT)
        row_frame.pack(fill="x", pady=1, padx=10, ipady=0)
        row_frame.grid_columnconfigure(0, weight=1); row_frame.grid_columnconfigure(1, weight=2)
        row_frame.grid_columnconfigure(2, weight=12); row_frame.grid_columnconfigure(3, weight=3)
        row_frame.grid_columnconfigure(4, weight=3)
        
        ctk.CTkLabel(row_frame, text=f"{index}", font=FONT_NORMAL, text_color=COLOR_TEXT_DEFAULT).grid(row=0, column=0)
        ctk.CTkLabel(row_frame, text=f"x{count}", font=FONT_BOLD, text_color=COLOR_TEXT_COUNT).grid(row=0, column=1)
        
        category_cell = ctk.CTkFrame(row_frame, fg_color="transparent")
        category_cell.grid(row=0, column=2, sticky="w")
        
        icon = self.get_icon_for_category(category)
        icon_label = ctk.CTkLabel(category_cell, text="", image=icon)
        icon_label.pack(side="left", padx=(5, 10))
        ctk.CTkLabel(category_cell, text=category, anchor="w", font=FONT_NORMAL, text_color=COLOR_TEXT_DEFAULT).pack(side="left")

        ctk.CTkLabel(row_frame, text=f"{active}", font=FONT_BOLD, text_color=COLOR_TEXT_ACTIVE).grid(row=0, column=3)
        ctk.CTkLabel(row_frame, text=f"{inactive}", font=FONT_BOLD, text_color=COLOR_TEXT_INACTIVE).grid(row=0, column=4)

        ctk.CTkFrame(self.results_frame, height=1, fg_color=COLOR_ROW_SEPARATOR).pack(fill="x", padx=5)

    def start_folder_selection_loop(self):
        self.selected_folders = []
        self.select_a_folder()

    def select_a_folder(self):
        folder = filedialog.askdirectory(title="Select a folder with TXT log files")
        if folder:
            if folder not in self.selected_folders:
                self.selected_folders.append(folder)
                self.status_bar.configure(text=f"{len(self.selected_folders)} folder(s) selected.")
            if messagebox.askyesno("Select More?", "Do you want to select another folder?"):
                self.select_a_folder()
            else:
                self.start_processing_thread()
        elif not self.selected_folders:
            self.status_bar.configure(text="Operation cancelled. No folders were selected.")

    def start_processing_thread(self):
        if not self.selected_folders:
            self.status_bar.configure(text="No folders selected to process.")
            return
        self.select_button.configure(state="disabled", text="Processing...")
        self.status_bar.configure(text=f"Processing {len(self.selected_folders)} folder(s), please wait...")
        threading.Thread(target=self.run_sorter, args=(self.selected_folders,), daemon=True).start()

    def run_sorter(self, folders_to_process):
        try:
            summary = self.process_logs(folders_to_process)
            self.after(10, self.populate_ui, summary)
            output_dir = os.path.join(folders_to_process[0], "skins_output")
            self.after(10, self.update_status, f"Success! Results saved in: {output_dir}", "idle")
        except Exception as e:
            self.after(10, self.update_status, f"An error occurred: {e}", "error")

    def update_status(self, message, state="idle"):
        self.status_bar.configure(text=message)
        if state in ["idle", "error"]:
            self.select_button.configure(state="normal", text="Select Folder(s) and Start Sorting")
        else:
            self.select_button.configure(state="disabled", text="Processing...")

    def get_category_priority(self, category_name):
        if category_name in self.vbuck_range_names: return 1
        if category_name in self.skin_range_names: return 2
        return 0

    def populate_ui(self, summary):
        self.clear_table()
        ctk.CTkFrame(self.results_frame, height=2, fg_color=COLOR_ROW_SEPARATOR).pack(fill="x", padx=5)

        self.active_label.configure(text=f"Total Active: {summary['total_active']}")
        self.inactive_label.configure(text=f"Total Inactive: {summary['total_inactive']}")

        all_categories = list(summary['skin_counts'].items()) + list(summary['vbucks_counts'].items()) + list(summary['skinrange_counts'].items())
        sorted_categories = sorted(
            [(cat, counts) for cat, counts in all_categories if counts['active'] > 0 or counts['inactive'] > 0],
            key=lambda item: (self.get_category_priority(item[0]), -(item[1]['active'] + item[1]['inactive']))
        )
        for i, (category, counts) in enumerate(sorted_categories, 1):
            self.add_table_row(i, counts['active'] + counts['inactive'], category, counts['active'], counts['inactive'])

    def process_logs(self, folder_list):
        # This function remains unchanged.
        rare_skins = ["Skull Trooper", "Wildcat", "Black Knight", "Galaxy", "IKONIK", "Sparkle Specialist", "World Warrior", "Travis Scott", "Glow", "Stealth Reflex", "Double Helix", "Blue Squire", "The Reaper", "Royale Bomber", "Psycho Bandit", "Elite Agent", "Trailblazer", "Dark Vertex", "Special Forces", "Neo Versa", "Rogue Spider Knight", "Dark Skully", "Dark Voyager", "Kratos", "Omega", "Huntmaster Saber", "Rose Team Leader", "Rogue Agent", "Polo Prodigy", "Renegade Raider", "Ghoul Trooper"]
        rare_pickaxes = ["Merry Mint Axe", "Leviathan Axe"]
        rare_gliders = ["Mako"]
        all_rare_items = rare_skins + rare_pickaxes + rare_gliders
        vbuck_ranges = {"1-50": (1, 50), "51-100": (51, 100), "100-200": (101, 200), "200-500": (201, 500), "500-2000": (501, 2000), "2000-5000": (2001, 5000), "5000+": (5001, float("inf"))}
        skin_ranges = {"6-20": (6, 20), "20-50": (21, 50), "50-100": (51, 100), "100-150": (101, 150), "150-200": (151, 200), "200+": (201, float("inf"))}
        output_dir = os.path.join(folder_list[0], "skins_output"); os.makedirs(output_dir, exist_ok=True)
        allowed_files = {"skins0to10.txt", "skins10to50.txt", "skins50plus.txt"}
        all_txt_files_to_process = [os.path.join(folder, f) for folder in folder_list for f in os.listdir(folder) if f in allowed_files and f.endswith(".txt")]
        if not all_txt_files_to_process: raise FileNotFoundError("No valid log files found in any of the selected directories.")
        accounts = {}
        for file_path in all_txt_files_to_process:
            with open(file_path, "r", encoding="utf-8", errors="ignore") as fh: content = fh.read()
            blocks = re.split(r"\n(?=[\w\.-]+@[\w\.-]+:)", content)
            for block in blocks:
                m = re.match(r"([\w\.-]+@[\w\.-]+:[^\s]+)", block)
                if not m: continue
                login = m.group(1).strip()
                acc = accounts.setdefault(login, {"found_items": set(), "vbucks": 0, "seen_active": False, "seen_inactive": False, "declared_skin_counts": []})
                char_m = re.search(r"Character:\s*([^\n\r]+)", block)
                if char_m:
                    characters = [c.strip().lower() for c in char_m.group(1).split(",") if c.strip()]
                    for skin_name in rare_skins:
                        if skin_name.lower() in characters: acc["found_items"].add(skin_name)
                pickaxe_m = re.search(r"Pickaxe:\s*([^\n\r]+)", block)
                if pickaxe_m:
                    pickaxes = [p.strip().lower() for p in pickaxe_m.group(1).split(",") if p.strip()]
                    for pickaxe_name in rare_pickaxes:
                        if pickaxe_name.lower() in pickaxes: acc["found_items"].add(pickaxe_name)
                glider_m = re.search(r"Glider:\s*([^\n\r]+)", block)
                if glider_m:
                    gliders = [g.strip().lower() for g in glider_m.group(1).split(",") if g.strip()]
                    for glider_name in rare_gliders:
                        if glider_name.lower() in gliders: acc["found_items"].add(glider_name)
                inactive_m = re.search(r"Inactive:\s*(true|false)", block, re.I)
                if inactive_m:
                    val = inactive_m.group(1).lower()
                    if val == "false": acc["seen_active"] = True
                    elif val == "true": acc["seen_inactive"] = True
                vb_m = re.search(r"VBucks:\s*(\d+)", block)
                if vb_m: acc["vbucks"] = max(acc["vbucks"], int(vb_m.group(1)))
                sdc_m = re.search(r"Skins:\s*(\d+)", block)
                if sdc_m: acc["declared_skin_counts"].append(int(sdc_m.group(1)))
        for acc in accounts.values():
            if acc["seen_active"]: acc["status"] = "active"
            elif acc["seen_inactive"]: acc["status"] = "inactive"
            else: acc["status"] = "active"
        item_counts = {item: {"active": 0, "inactive": 0} for item in all_rare_items}
        vbucks_counts = {k: {"active": 0, "inactive": 0} for k in vbuck_ranges.keys()}
        skinrange_counts = {k: {"active": 0, "inactive": 0} for k in skin_ranges.keys()}
        item_files = {item: set() for item in all_rare_items}; vbucks_files = {k: set() for k in vbuck_ranges.keys()}; skinrange_files = {k: set() for k in skin_ranges.keys()}
        for login, acc in accounts.items():
            key = "active" if acc["status"] == "active" else "inactive"
            for item_name in acc["found_items"]:
                if item_name in item_counts:
                    item_counts[item_name][key] += 1
                    item_files[item_name].add(login)
            v = acc["vbucks"]
            if v > 0:
                for label, (mn, mx) in vbuck_ranges.items():
                    if mn <= v <= mx: vbucks_counts[label][key] += 1; vbucks_files[label].add(login); break
            scount = max(acc["declared_skin_counts"]) if acc["declared_skin_counts"] else len(acc["found_items"])
            for label, (mn, mx) in skin_ranges.items():
                if mn <= scount <= mx: skinrange_counts[label][key] += 1; skinrange_files[label].add(login); break
        for item_name, sset in item_files.items():
            if sset:
                fname = item_name.lower().replace(" ", "_") + ".txt"
                with open(os.path.join(output_dir, fname), "w", encoding="utf-8") as out:
                    for l in sorted(sset): out.write(l + "\n")
        for label, sset in vbucks_files.items():
            if sset:
                fname = f"vbucks_{label.replace('+','plus').replace(' ','')}.txt"
                with open(os.path.join(output_dir, fname), "w", encoding="utf-8") as out:
                    for l in sorted(sset): out.write(l + "\n")
        for label, sset in skinrange_files.items():
            if sset:
                fname = f"skins_{label.replace('+','plus').replace(' ','')}.txt"
                with open(os.path.join(output_dir, fname), "w", encoding="utf-8") as out:
                    for l in sorted(sset): out.write(l + "\n")
        summary = {"total_accounts": len(accounts), "total_active": sum(1 for a in accounts.values() if a["status"] == "active"), "total_inactive": sum(1 for a in accounts.values() if a["status"] == "inactive"), "skin_counts": item_counts, "vbucks_counts": vbucks_counts, "skinrange_counts": skinrange_counts}
        with open(os.path.join(output_dir, "summary.json"), "w", encoding="utf-8") as jf: json.dump(summary, jf, indent=2)
        return summary

if __name__ == "__main__":
    app = App()
    app.mainloop()