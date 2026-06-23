# ShopDraw

## 🚀 Revit Automation: 3D Generation from JSON Data

### 📌 Feature Overview (Tổng quan tính năng)
This feature fully automates the 3D generation of MEP systems (Piping/Ducting) within Revit directly from an input JSON configuration file. Instead of spending hours manually modeling each component, the Add-in leverages advanced geometric algorithms to calculate coordinates and resolve connections in a matter of seconds.

Tính năng này tự động hóa hoàn toàn quy trình dựng mô hình 3D (hệ thống đường ống/ống gió) trong Revit trực tiếp từ dữ liệu cấu hình đầu vào dạng **JSON**. Thay vì phải mô hình hóa thủ công từng cấu kiện tốn hàng giờ đồng hồ, Add-in xử lý chính xác các thuật toán hình học để sinh tọa độ, kết nối cấu kiện chỉ trong vài giây.

### 🎥 Demo Video (Demo hoạt động)
<img width="800" height="450" alt="ezgif-7cce81484dcbae34" src="https://github.com/user-attachments/assets/11d390d4-2abe-41cd-b30a-d474476486e8" />

### 🛠️ Input Data Structure (Cấu trúc dữ liệu đầu vào) (JSON Example)
```json
{
  "Levels": [
    { "Name": "Level 1", "Elevation": 0.0 }
  ],
  "Curves": [
    {
      "Id": "Pipe_01",
      "Type": "Chilled Water Pipe",
      "StartPoint": { "X": 0.0, "Y": 0.0, "Z": 0.0 },
      "EndPoint": { "X": 3000.0, "Y": 0.0, "Z": 0.0 },
      "Diameter": 100.0,
      "Level": "Level 1"
    }
  ],
  "Fittings": [
    {
      "Type": "Elbow 90",
      "Location": { "X": 3000.0, "Y": 0.0, "Z": 0.0 },
      "ConnectedElements": ["Pipe_01", "Pipe_02"]
    }
  ]
}
```

### ⚡ Technical Highlights & Impact (Điểm nhấn kỹ thuật & Hiệu suất) (ROI)
- Performance: Reduces modeling time by 95% compared to traditional manual workflows (Generates hundreds of elements under 5 seconds).
- Geometric Precision: Automatically evaluates orientation vectors (BasisX, BasisY) and rotation parameters (Beta angle) to place Fittings accurately at intersections, minimizing structural and MEP hard clashes.
- Tech Stack: C#, .NET Framework, Revit API, Newtonsoft.Json.

- Tốc độ xử lý: Giảm 95% thời gian so với việc dựng hình thủ công (Xử lý hàng trăm cấu kiện trong < 5 giây).
- Thuật toán hình học: Tự động tính toán vector hướng (BasisX, BasisY) và góc xoay (Beta angle) để đặt các Fitting chính xác tại điểm giao, loại bỏ hoàn toàn lỗi va chạm hình học (Hard Clashes).
- Công nghệ sử dụng: C#, .NET Framework, Revit API, Newtonsoft.Json.
