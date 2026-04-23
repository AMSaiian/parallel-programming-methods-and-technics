from datasets import load_dataset
import os

output_dir = "../Static/html"
os.makedirs(output_dir, exist_ok=True)

print("Connecting to Hugging Face")

dataset = load_dataset(
    "ddrg/super_eurlex", 
    name="1.EN.html",
    split="train", 
    streaming=True,
    trust_remote_code=True
)

file_count = 0
for row in dataset:
    if file_count >= 10000:
        break
        
    html_content = row.get('text_html_raw')
    
    if html_content:
        filepath = os.path.join(output_dir, f"document_{file_count + 1}.html")
        
        with open(filepath, "w", encoding="utf-8") as file:
            file.write(html_content)
            
        file_count += 1

print(f"Saved {file_count} HTML files to the '{output_dir}' folder.")
