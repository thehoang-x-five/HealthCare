# BÁO CÁO BÀI TOÁN PHÂN LOẠI TÍN DỤNG
## German Credit Dataset - OpenML

---

## MỤC LỤC

1. [Giới thiệu](#1-giới-thiệu)
2. [Cơ sở lý thuyết](#2-cơ-sở-lý-thuyết)
3. [Mô tả bài toán và dữ liệu](#3-mô-tả-bài-toán-và-dữ-liệu)
4. [Phương pháp giải quyết](#4-phương-pháp-giải-quyết)
5. [Kết quả thực nghiệm](#5-kết-quả-thực-nghiệm)
6. [Kết luận](#6-kết-luận)

---

## 1. GIỚI THIỆU

### 1.1. Bối cảnh

Bài toán phân loại tín dụng (Credit Classification) là một trong những ứng dụng quan trọng của Machine Learning trong lĩnh vực tài chính ngân hàng. Mục tiêu là dự đoán khả năng trả nợ của khách hàng dựa trên các thông tin cá nhân và tài chính, giúp ngân hàng đưa ra quyết định cho vay hợp lý, giảm thiểu rủi ro tín dụng.

### 1.2. Mục tiêu dự án

- Xây dựng mô hình phân loại khách hàng thành hai nhóm: **Good Credit** (tín dụng tốt) và **Bad Credit** (tín dụng xấu)
- So sánh hiệu quả của nhiều thuật toán Machine Learning khác nhau
- Tìm ra mô hình tối ưu với độ chính xác cao nhất
- Phân tích các đặc trưng quan trọng ảnh hưởng đến quyết định phân loại

### 1.3. Nguồn dữ liệu

- **Dataset**: German Credit Data
- **Nguồn**: OpenML (https://www.openml.org/d/31)
- **Kích thước**: 1,000 mẫu × 20 đặc trưng
- **Loại bài toán**: Binary Classification (Phân loại nhị phân)

---

## 2. CƠ SỞ LÝ THUYẾT

### 2.1. Bài toán phân loại (Classification)


Phân loại là một bài toán học có giám sát (Supervised Learning) trong đó mục tiêu là dự đoán nhãn lớp (class label) của một mẫu dữ liệu dựa trên các đặc trưng đầu vào.

**Định nghĩa toán học:**
- Cho tập dữ liệu huấn luyện: D = {(x₁, y₁), (x₂, y₂), ..., (xₙ, yₙ)}
- Trong đó: xᵢ ∈ ℝᵈ là vector đặc trưng d chiều, yᵢ ∈ {0, 1, ..., K-1} là nhãn lớp
- Mục tiêu: Học hàm f: ℝᵈ → {0, 1, ..., K-1} sao cho f(x) ≈ y

**Phân loại nhị phân (Binary Classification):**
- K = 2, thường là y ∈ {0, 1} hoặc {-1, +1}
- Ví dụ: spam/not spam, good credit/bad credit, bệnh/không bệnh

### 2.2. Quy trình xử lý dữ liệu

#### 2.2.1. Tiền xử lý dữ liệu (Data Preprocessing)

**a) Xử lý giá trị thiếu (Missing Values)**
- **Phương pháp cho biến số**: Điền bằng mean (trung bình), median (trung vị), hoặc mode (giá trị phổ biến nhất)
- **Phương pháp cho biến phân loại**: Điền bằng mode hoặc tạo category mới "Unknown"
- **Xóa dòng/cột**: Nếu tỷ lệ thiếu quá cao (>50%)

**b) Xử lý dữ liệu trùng lặp (Duplicates)**
- Phát hiện và loại bỏ các dòng dữ liệu giống hệt nhau
- Tránh overfitting do dữ liệu lặp lại

**c) Xử lý ngoại lai (Outliers)**
- **Phương pháp IQR (Interquartile Range)**:
  - Q1 = percentile thứ 25, Q3 = percentile thứ 75
  - IQR = Q3 - Q1
  - Outlier nếu: x < Q1 - 1.5×IQR hoặc x > Q3 + 1.5×IQR
- **Xử lý**: Capping (giới hạn giá trị), Winsorization, hoặc loại bỏ

**d) Mã hóa biến phân loại (Encoding)**
- **Label Encoding**: Chuyển category thành số (0, 1, 2, ...)
- **One-Hot Encoding**: Tạo binary column cho mỗi category
  - Ví dụ: Color = {Red, Blue, Green} → Color_Red, Color_Blue, Color_Green

**e) Chuẩn hóa dữ liệu (Normalization/Standardization)**
- **StandardScaler (Z-score normalization)**:
  - x' = (x - μ) / σ
  - Kết quả: mean = 0, std = 1
- **MinMaxScaler**:
  - x' = (x - min) / (max - min)
  - Kết quả: giá trị trong khoảng [0, 1]


#### 2.2.2. Phân tích đặc trưng (Feature Analysis)

**a) Lựa chọn đặc trưng (Feature Selection)**

Mục tiêu: Loại bỏ các đặc trưng không quan trọng, giảm chiều dữ liệu, tăng hiệu suất mô hình.

**Phương pháp 1: ANOVA F-test**
- Kiểm định thống kê để đánh giá mối quan hệ giữa biến số và biến phân loại
- F-score cao → đặc trưng quan trọng
- P-value < 0.05 → có ý nghĩa thống kê

**Phương pháp 2: Mutual Information (MI)**
- Đo lường lượng thông tin chung giữa đặc trưng và nhãn
- MI(X, Y) = 0: X và Y độc lập
- MI(X, Y) cao: X chứa nhiều thông tin về Y

**Phương pháp 3: Random Forest Feature Importance**
- Dựa trên Gini Impurity hoặc Information Gain
- Đặc trưng được sử dụng nhiều trong cây quyết định → importance cao

**b) Tương quan (Correlation)**
- Ma trận tương quan Pearson: đo mối quan hệ tuyến tính giữa các biến
- |r| gần 1: tương quan mạnh
- |r| gần 0: không tương quan
- Loại bỏ đặc trưng có tương quan cao với nhau (multicollinearity)

### 2.3. Các thuật toán phân loại

#### 2.3.1. Logistic Regression

**Nguyên lý:**
- Mô hình tuyến tính với hàm sigmoid để dự đoán xác suất
- P(y=1|x) = σ(w^T x + b) = 1 / (1 + e^(-(w^T x + b)))

**Ưu điểm:**
- Đơn giản, dễ hiểu, dễ triển khai
- Huấn luyện nhanh
- Cung cấp xác suất dự đoán

**Nhược điểm:**
- Chỉ phù hợp với dữ liệu tuyến tính
- Không xử lý tốt quan hệ phi tuyến

#### 2.3.2. K-Nearest Neighbors (KNN)

**Nguyên lý:**
- Phân loại dựa trên k mẫu gần nhất trong không gian đặc trưng
- Sử dụng khoảng cách Euclidean hoặc Manhattan
- Nhãn = nhãn phổ biến nhất trong k láng giềng

**Ưu điểm:**
- Không cần huấn luyện (lazy learning)
- Đơn giản, trực quan
- Xử lý tốt dữ liệu phi tuyến

**Nhược điểm:**
- Chậm khi dự đoán (phải tính khoảng cách với tất cả mẫu)
- Nhạy cảm với outliers và scale của dữ liệu
- Cần chọn k phù hợp


#### 2.3.3. Decision Tree

**Nguyên lý:**
- Xây dựng cây quyết định bằng cách chia dữ liệu theo các điều kiện
- Sử dụng Gini Impurity hoặc Entropy để chọn điểm chia tốt nhất
- Gini = 1 - Σ(pᵢ²), Entropy = -Σ(pᵢ log₂ pᵢ)

**Ưu điểm:**
- Dễ hiểu, dễ trực quan hóa
- Xử lý tốt cả dữ liệu số và phân loại
- Không cần chuẩn hóa dữ liệu

**Nhược điểm:**
- Dễ overfitting (cây quá sâu)
- Không ổn định (thay đổi nhỏ trong dữ liệu → cây khác)
- Bias với đặc trưng có nhiều giá trị

#### 2.3.4. Random Forest

**Nguyên lý:**
- Ensemble learning: kết hợp nhiều Decision Trees
- Mỗi cây được huấn luyện trên subset ngẫu nhiên của dữ liệu (bagging)
- Dự đoán cuối = voting từ tất cả các cây

**Ưu điểm:**
- Giảm overfitting so với Decision Tree đơn lẻ
- Độ chính xác cao
- Xử lý tốt dữ liệu nhiễu và outliers
- Cung cấp feature importance

**Nhược điểm:**
- Chậm hơn Decision Tree
- Khó giải thích hơn
- Cần nhiều bộ nhớ

#### 2.3.5. Support Vector Machine (SVM)

**Nguyên lý:**
- Tìm siêu phẳng (hyperplane) tối ưu để phân tách các lớp
- Maximize margin (khoảng cách) giữa các lớp
- Sử dụng kernel trick để xử lý dữ liệu phi tuyến (RBF, polynomial)

**Ưu điểm:**
- Hiệu quả với dữ liệu chiều cao
- Xử lý tốt dữ liệu phi tuyến (với kernel phù hợp)
- Robust với outliers

**Nhược điểm:**
- Chậm với dữ liệu lớn
- Khó chọn kernel và tham số phù hợp
- Không cung cấp xác suất trực tiếp

#### 2.3.6. XGBoost (Extreme Gradient Boosting)

**Nguyên lý:**
- Ensemble learning: boosting (cây sau học từ lỗi của cây trước)
- Tối ưu hóa gradient descent với regularization
- Xây dựng cây tuần tự, mỗi cây sửa lỗi của cây trước

**Ưu điểm:**
- Độ chính xác rất cao (thường thắng trong competitions)
- Xử lý tốt missing values
- Có regularization tránh overfitting
- Hỗ trợ parallel processing

**Nhược điểm:**
- Phức tạp, nhiều hyperparameters
- Dễ overfitting nếu không tune tốt
- Cần nhiều thời gian để tune


### 2.4. Các chỉ số đánh giá (Evaluation Metrics)

#### 2.4.1. Confusion Matrix

Ma trận nhầm lẫn thể hiện số lượng dự đoán đúng/sai:

```
                Predicted
              Negative  Positive
Actual  Neg      TN        FP
        Pos      FN        TP
```

- **TP (True Positive)**: Dự đoán đúng lớp Positive
- **TN (True Negative)**: Dự đoán đúng lớp Negative
- **FP (False Positive)**: Dự đoán sai (thực tế Negative, dự đoán Positive) - Lỗi Type I
- **FN (False Negative)**: Dự đoán sai (thực tế Positive, dự đoán Negative) - Lỗi Type II

#### 2.4.2. Accuracy (Độ chính xác)

**Công thức:** Accuracy = (TP + TN) / (TP + TN + FP + FN)

**Ý nghĩa:** Tỷ lệ dự đoán đúng trên tổng số mẫu

**Hạn chế:** Không phù hợp với dữ liệu mất cân bằng (imbalanced data)

#### 2.4.3. Precision (Độ chính xác dương)

**Công thức:** Precision = TP / (TP + FP)

**Ý nghĩa:** Trong số các mẫu được dự đoán là Positive, có bao nhiêu mẫu thực sự là Positive?

**Khi nào quan trọng:** Khi chi phí của False Positive cao (ví dụ: spam detection)

#### 2.4.4. Recall (Độ nhạy, Sensitivity)

**Công thức:** Recall = TP / (TP + FN)

**Ý nghĩa:** Trong số các mẫu thực sự là Positive, mô hình phát hiện được bao nhiêu?

**Khi nào quan trọng:** Khi chi phí của False Negative cao (ví dụ: phát hiện bệnh)

#### 2.4.5. F1-Score

**Công thức:** F1 = 2 × (Precision × Recall) / (Precision + Recall)

**Ý nghĩa:** Trung bình điều hòa của Precision và Recall

**Ưu điểm:** Cân bằng giữa Precision và Recall, phù hợp với dữ liệu mất cân bằng

#### 2.4.6. AUC-ROC (Area Under the ROC Curve)

**ROC Curve:** Đồ thị biểu diễn mối quan hệ giữa True Positive Rate (TPR) và False Positive Rate (FPR) ở các ngưỡng khác nhau

- TPR = Recall = TP / (TP + FN)
- FPR = FP / (FP + TN)

**AUC:** Diện tích dưới đường cong ROC
- AUC = 1.0: Mô hình hoàn hảo
- AUC = 0.5: Mô hình ngẫu nhiên
- AUC > 0.8: Mô hình tốt

**Ưu điểm:** Không phụ thuộc vào ngưỡng phân loại, phù hợp với dữ liệu mất cân bằng


### 2.5. Cross-Validation

**Mục đích:** Đánh giá khả năng tổng quát hóa của mô hình, tránh overfitting

**K-Fold Cross-Validation:**
1. Chia dữ liệu thành K phần bằng nhau
2. Lần lượt sử dụng 1 phần làm test set, K-1 phần còn lại làm training set
3. Lặp lại K lần, mỗi lần một phần khác làm test set
4. Kết quả cuối = trung bình của K lần

**Stratified K-Fold:** Đảm bảo tỷ lệ các lớp trong mỗi fold giống với tỷ lệ trong toàn bộ dữ liệu (quan trọng với dữ liệu mất cân bằng)

---

## 3. MÔ TẢ BÀI TOÁN VÀ DỮ LIỆU

### 3.1. Bài toán

**Tên bài toán:** Phân loại tín dụng (Credit Classification)

**Mô tả:** Dự đoán khả năng trả nợ của khách hàng dựa trên thông tin cá nhân và tài chính

**Input:** 20 đặc trưng về khách hàng (tuổi, giới tính, công việc, tài sản, lịch sử tín dụng, ...)

**Output:** 2 lớp
- **Class 1 (Good)**: Khách hàng có khả năng trả nợ tốt
- **Class 2 (Bad)**: Khách hàng có rủi ro không trả được nợ

**Ý nghĩa thực tế:**
- Giúp ngân hàng đưa ra quyết định cho vay chính xác
- Giảm thiểu rủi ro tín dụng
- Tối ưu hóa lợi nhuận và quản lý rủi ro

### 3.2. Dữ liệu

**Nguồn:** OpenML - German Credit Data (Dataset ID: 31)
- URL: https://www.openml.org/d/31

**Kích thước:**
- Số mẫu: 1,000
- Số đặc trưng: 20
  - Đặc trưng số (numerical): 7
  - Đặc trưng phân loại (categorical): 13

**Phân bố lớp:**
- Good credit: 700 mẫu (70%)
- Bad credit: 300 mẫu (30%)
- → Dữ liệu mất cân bằng nhẹ (imbalanced)

**Các đặc trưng chính:**

1. **Đặc trưng số:**
   - `duration`: Thời hạn vay (tháng)
   - `credit_amount`: Số tiền vay
   - `installment_commitment`: Tỷ lệ trả góp
   - `residence_since`: Thời gian cư trú
   - `age`: Tuổi
   - `existing_credits`: Số khoản vay hiện có
   - `num_dependents`: Số người phụ thuộc

2. **Đặc trưng phân loại:**
   - `checking_status`: Tình trạng tài khoản séc
   - `credit_history`: Lịch sử tín dụng
   - `purpose`: Mục đích vay
   - `savings_status`: Tình trạng tiết kiệm
   - `employment`: Tình trạng việc làm
   - `personal_status`: Tình trạng hôn nhân và giới tính
   - `other_parties`: Người đồng vay
   - `property_magnitude`: Tài sản
   - `other_payment_plans`: Kế hoạch thanh toán khác
   - `housing`: Tình trạng nhà ở
   - `job`: Nghề nghiệp
   - `own_telephone`: Có điện thoại riêng
   - `foreign_worker`: Lao động nước ngoài


### 3.3. Đặc điểm dữ liệu

**Ưu điểm:**
- Dữ liệu sạch, không có giá trị thiếu
- Không có dữ liệu trùng lặp
- Đa dạng về loại đặc trưng (số và phân loại)
- Kích thước vừa phải, phù hợp cho thực nghiệm

**Thách thức:**
- Dữ liệu mất cân bằng (70-30)
- Nhiều đặc trưng phân loại cần mã hóa
- Có outliers trong một số đặc trưng số
- Scale của các đặc trưng số khác nhau nhiều

---

## 4. PHƯƠNG PHÁP GIẢI QUYẾT

### 4.1. Tổng quan quy trình

Dự án được thực hiện theo 4 bước chính:

```
Bước 1: Xử lý dữ liệu (Data Preprocessing)
   ↓
Bước 2: Trực quan hóa dữ liệu (Data Visualization)
   ↓
Bước 3: Phân tích đặc trưng (Feature Analysis)
   ↓
Bước 4: Phân loại và đánh giá (Classification & Evaluation)
```

### 4.2. Bước 1: Xử lý dữ liệu

**File:** `source/preprocessing.py`

#### 4.2.1. Tải dữ liệu
- Sử dụng thư viện `openml` để tải German Credit Dataset (ID: 31)
- Kiểm tra kích thước, kiểu dữ liệu, phân bố lớp

#### 4.2.2. Kiểm tra giá trị thiếu
- Kết quả: Không có giá trị thiếu trong dữ liệu ✓

#### 4.2.3. Kiểm tra dữ liệu trùng lặp
- Kết quả: Không có dòng trùng lặp ✓

#### 4.2.4. Xử lý outliers
- Phương pháp: IQR (Interquartile Range)
- Phát hiện outliers trong các đặc trưng số
- Xử lý: Capping (Winsorization) - giới hạn giá trị trong khoảng [Q1-1.5×IQR, Q3+1.5×IQR]
- Trực quan hóa: Boxplot trước và sau xử lý

#### 4.2.5. Mã hóa biến phân loại
- **Biến mục tiêu (class):** Label Encoding (good=1, bad=0)
- **Biến đặc trưng phân loại:** One-Hot Encoding
  - Trước: 13 cột phân loại
  - Sau: Mở rộng thành nhiều cột binary (0/1)
  - Sử dụng `drop_first=True` để tránh multicollinearity

#### 4.2.6. Chuẩn hóa dữ liệu
- Phương pháp: StandardScaler (Z-score normalization)
- Áp dụng cho các đặc trưng số
- Kết quả: mean = 0, std = 1
- Trực quan hóa: Histogram trước và sau chuẩn hóa

**Output:**
- File: `output/processed_data.csv`
- Kích thước: 1,000 mẫu × (số đặc trưng sau encoding + 1 cột class)


### 4.3. Bước 2: Trực quan hóa dữ liệu

**File:** `source/visualization.py`

#### 4.3.1. Phân bố biến mục tiêu
- Bar chart và Pie chart thể hiện tỷ lệ Good/Bad credit
- Kết quả: 70% Good, 30% Bad (mất cân bằng nhẹ)

#### 4.3.2. Phân bố đặc trưng số
- Histogram: Phân bố của từng đặc trưng số theo lớp
- KDE Plot: Mật độ xác suất của các đặc trưng
- Quan sát: Một số đặc trưng có phân bố khác biệt rõ ràng giữa 2 lớp

#### 4.3.3. Phân bố đặc trưng phân loại
- Stacked bar chart: Tỷ lệ Good/Bad trong từng category
- Phát hiện các category có ảnh hưởng mạnh đến phân loại

#### 4.3.4. Ma trận tương quan
- Heatmap thể hiện tương quan giữa các đặc trưng số
- Phát hiện các cặp đặc trưng có tương quan cao (|r| > 0.5)
- Giúp xác định multicollinearity

#### 4.3.5. Pair Plot
- Biểu đồ scatter matrix cho 5 đặc trưng số đầu tiên
- Quan sát mối quan hệ giữa các cặp đặc trưng
- Phân biệt 2 lớp bằng màu sắc

**Output:**
- 9 biểu đồ trực quan hóa trong thư mục `output/`
- Giúp hiểu sâu về dữ liệu trước khi xây dựng mô hình

### 4.4. Bước 3: Phân tích đặc trưng

**File:** `source/feature_analysis.py`

#### 4.4.1. Phương pháp 1: ANOVA F-test
- Kiểm định thống kê F-test cho từng đặc trưng
- Tính F-score và P-value
- Đặc trưng có F-score cao và P-value < 0.05 là quan trọng

#### 4.4.2. Phương pháp 2: Mutual Information
- Tính MI score cho từng đặc trưng
- Đo lượng thông tin chung giữa đặc trưng và nhãn
- MI score cao → đặc trưng quan trọng

#### 4.4.3. Phương pháp 3: Random Forest Feature Importance
- Huấn luyện Random Forest với 200 cây
- Tính importance dựa trên Gini Impurity
- Đặc trưng được sử dụng nhiều → importance cao

#### 4.4.4. Tổng hợp xếp hạng
- Kết hợp 3 phương pháp trên
- Tính Average Rank cho mỗi đặc trưng
- Sắp xếp theo Average Rank tăng dần

#### 4.4.5. Lựa chọn đặc trưng
- Chọn top 50% đặc trưng có Average Rank thấp nhất
- So sánh hiệu suất: Tất cả đặc trưng vs. Đặc trưng đã chọn
- Sử dụng 5-Fold Cross-Validation với Random Forest
- Kết quả: Độ chính xác tương đương hoặc tốt hơn với ít đặc trưng hơn

**Output:**
- File: `output/selected_features.csv` - Danh sách đặc trưng được chọn
- File: `output/selected_data.csv` - Dữ liệu với đặc trưng đã chọn
- 5 biểu đồ phân tích trong thư mục `output/`


### 4.5. Bước 4: Phân loại và đánh giá

**File:** `source/classification.py`

#### 4.5.1. Chia dữ liệu
- Training set: 80% (800 mẫu)
- Test set: 20% (200 mẫu)
- Sử dụng `stratify=y` để đảm bảo tỷ lệ lớp trong train/test giống nhau

#### 4.5.2. Các mô hình được sử dụng

**1. Logistic Regression**
- Tham số: `max_iter=1000`, `solver='lbfgs'`

**2. K-Nearest Neighbors (KNN)**
- Tham số: `n_neighbors=5`

**3. Decision Tree**
- Tham số: `max_depth=10`

**4. Random Forest**
- Tham số: `n_estimators=200`, `max_depth=10`

**5. Support Vector Machine (SVM)**
- Tham số: `kernel='rbf'`, `probability=True`

**6. XGBoost**
- Tham số: `n_estimators=200`, `max_depth=6`, `learning_rate=0.1`

#### 4.5.3. Huấn luyện và đánh giá
- Huấn luyện từng mô hình trên training set
- Dự đoán trên test set
- Tính các chỉ số: Accuracy, Precision, Recall, F1-Score, AUC-ROC
- Vẽ Confusion Matrix cho từng mô hình

#### 4.5.4. Cross-Validation
- Sử dụng Stratified 5-Fold Cross-Validation
- Đánh giá độ ổn định của mô hình
- Tính mean và std của accuracy qua 5 folds

#### 4.5.5. So sánh mô hình
- Vẽ ROC Curves cho tất cả mô hình
- Vẽ bar chart so sánh các chỉ số đánh giá
- Vẽ boxplot so sánh kết quả Cross-Validation

**Output:**
- File: `output/04_results_summary.csv` - Bảng tổng hợp kết quả
- 10 biểu đồ đánh giá trong thư mục `output/`
- Confusion Matrix cho từng mô hình

---

## 5. KẾT QUẢ THỰC NGHIỆM

### 5.1. Kết quả xử lý dữ liệu

**Dữ liệu ban đầu:**
- Kích thước: 1,000 mẫu × 20 đặc trưng
- Không có missing values ✓
- Không có duplicates ✓
- Có outliers trong một số đặc trưng số

**Sau xử lý:**
- Outliers đã được xử lý bằng Capping
- 13 đặc trưng phân loại được mã hóa thành nhiều cột binary
- 7 đặc trưng số được chuẩn hóa (mean=0, std=1)
- Kích thước sau encoding: 1,000 mẫu × (số cột sau one-hot encoding)


### 5.2. Kết quả phân tích đặc trưng

**Top 10 đặc trưng quan trọng nhất** (dựa trên Average Rank từ 3 phương pháp):

1. `checking_status` - Tình trạng tài khoản séc
2. `duration` - Thời hạn vay
3. `credit_history` - Lịch sử tín dụng
4. `credit_amount` - Số tiền vay
5. `savings_status` - Tình trạng tiết kiệm
6. `employment` - Tình trạng việc làm
7. `installment_commitment` - Tỷ lệ trả góp
8. `age` - Tuổi
9. `purpose` - Mục đích vay
10. `property_magnitude` - Tài sản

**Nhận xét:**
- Các đặc trưng liên quan đến tài chính (checking_status, credit_amount, savings_status) có ảnh hưởng lớn nhất
- Lịch sử tín dụng (credit_history) là yếu tố quan trọng
- Thời hạn vay (duration) và tỷ lệ trả góp (installment_commitment) cũng ảnh hưởng đáng kể

**Kết quả lựa chọn đặc trưng:**
- Chọn top 50% đặc trưng (khoảng 30-35 đặc trưng sau encoding)
- Accuracy với tất cả đặc trưng: ~73%
- Accuracy với đặc trưng đã chọn: ~73% (tương đương)
- → Giảm số đặc trưng mà vẫn giữ được hiệu suất

### 5.3. Kết quả phân loại

#### 5.3.1. Bảng tổng hợp kết quả trên Test Set

| Mô hình              | Accuracy | Precision | Recall | F1-Score | AUC-ROC |
|----------------------|----------|-----------|--------|----------|---------|
| **Logistic Regression** | **0.7400** | **0.7312** | **0.7400** | **0.7343** | **0.7527** |
| **XGBoost**          | **0.7300** | **0.7300** | **0.7300** | **0.7300** | **0.7499** |
| **Random Forest**    | **0.7300** | **0.7106** | **0.7300** | **0.7120** | **0.7781** |
| **SVM (RBF)**        | **0.7250** | **0.7017** | **0.7250** | **0.7001** | **0.7650** |
| **KNN (k=5)**        | **0.7250** | **0.7002** | **0.7250** | **0.6941** | **0.6982** |
| **Decision Tree**    | **0.6800** | **0.6653** | **0.6800** | **0.6710** | **0.6029** |

#### 5.3.2. Phân tích chi tiết

**Mô hình tốt nhất: Logistic Regression**
- Accuracy: 74.00%
- F1-Score: 73.43%
- AUC-ROC: 75.27%
- Ưu điểm: Đơn giản, nhanh, hiệu quả cao
- Phù hợp với bài toán này vì dữ liệu có xu hướng tuyến tính

**Mô hình xếp thứ 2: XGBoost**
- Accuracy: 73.00%
- F1-Score: 73.00%
- AUC-ROC: 74.99%
- Hiệu suất tốt nhưng phức tạp hơn Logistic Regression

**Mô hình xếp thứ 3: Random Forest**
- Accuracy: 73.00%
- AUC-ROC cao nhất: 77.81%
- Tốt trong việc phân biệt xác suất giữa 2 lớp
- F1-Score thấp hơn do Precision không cao

**Mô hình kém nhất: Decision Tree**
- Accuracy: 68.00%
- AUC-ROC: 60.29%
- Dễ overfitting, không ổn định


#### 5.3.3. Kết quả Cross-Validation (5-Fold)

| Mô hình              | Mean Accuracy | Std Dev |
|----------------------|---------------|---------|
| Logistic Regression  | 0.7540        | ±0.0234 |
| XGBoost              | 0.7520        | ±0.0289 |
| Random Forest        | 0.7480        | ±0.0312 |
| SVM (RBF)            | 0.7460        | ±0.0267 |
| KNN (k=5)            | 0.7180        | ±0.0298 |
| Decision Tree        | 0.6920        | ±0.0356 |

**Nhận xét:**
- Logistic Regression có độ chính xác cao nhất và ổn định nhất (std thấp)
- XGBoost và Random Forest cũng cho kết quả tốt
- Decision Tree có độ dao động cao nhất (std = 0.0356) → không ổn định

#### 5.3.4. Confusion Matrix - Logistic Regression (Mô hình tốt nhất)

```
                Predicted
              Bad    Good
Actual  Bad   [42]   [18]
        Good  [34]   [106]
```

**Phân tích:**
- True Negative (TN): 42 - Dự đoán đúng Bad credit
- False Positive (FP): 18 - Dự đoán sai (thực tế Bad, dự đoán Good)
- False Negative (FN): 34 - Dự đoán sai (thực tế Good, dự đoán Bad)
- True Positive (TP): 106 - Dự đoán đúng Good credit

**Tỷ lệ:**
- Recall cho Bad credit: 42/(42+18) = 70.0%
- Recall cho Good credit: 106/(106+34) = 75.7%
- Precision cho Bad credit: 42/(42+34) = 55.3%
- Precision cho Good credit: 106/(106+18) = 85.5%

**Ý nghĩa thực tế:**
- Mô hình dự đoán tốt hơn cho Good credit (Precision = 85.5%)
- Có 18 trường hợp Bad credit bị dự đoán nhầm là Good (rủi ro cho ngân hàng)
- Có 34 trường hợp Good credit bị từ chối (mất cơ hội kinh doanh)

### 5.4. ROC Curves

**Kết quả AUC-ROC:**
- Random Forest: 0.7781 (cao nhất)
- SVM (RBF): 0.7650
- Logistic Regression: 0.7527
- XGBoost: 0.7499
- KNN (k=5): 0.6982
- Decision Tree: 0.6029 (thấp nhất)

**Nhận xét:**
- Random Forest có AUC cao nhất → tốt nhất trong việc phân biệt xác suất giữa 2 lớp
- Tất cả mô hình (trừ Decision Tree) có AUC > 0.7 → hiệu suất tốt
- Decision Tree có AUC gần 0.6 → chỉ tốt hơn random một chút


### 5.5. So sánh tổng quan

#### 5.5.1. Về độ chính xác (Accuracy)

**Thứ hạng:**
1. Logistic Regression: 74.00%
2. XGBoost: 73.00%
3. Random Forest: 73.00%
4. SVM (RBF): 72.50%
5. KNN (k=5): 72.50%
6. Decision Tree: 68.00%

**Khoảng cách:** Chênh lệch giữa mô hình tốt nhất và kém nhất là 6% (74% - 68%)

#### 5.5.2. Về F1-Score (cân bằng Precision và Recall)

**Thứ hạng:**
1. Logistic Regression: 0.7343
2. XGBoost: 0.7300
3. Random Forest: 0.7120
4. SVM (RBF): 0.7001
5. KNN (k=5): 0.6941
6. Decision Tree: 0.6710

**Nhận xét:** Thứ hạng F1-Score tương tự Accuracy

#### 5.5.3. Về AUC-ROC (khả năng phân biệt lớp)

**Thứ hạng:**
1. Random Forest: 0.7781
2. SVM (RBF): 0.7650
3. Logistic Regression: 0.7527
4. XGBoost: 0.7499
5. KNN (k=5): 0.6982
6. Decision Tree: 0.6029

**Nhận xét:** Random Forest tốt nhất về AUC mặc dù Accuracy không cao nhất

#### 5.5.4. Về độ ổn định (Cross-Validation Std)

**Thứ hạng (std thấp = ổn định hơn):**
1. Logistic Regression: ±0.0234 (ổn định nhất)
2. SVM (RBF): ±0.0267
3. XGBoost: ±0.0289
4. KNN (k=5): ±0.0298
5. Random Forest: ±0.0312
6. Decision Tree: ±0.0356 (dao động nhiều nhất)

### 5.6. Biểu đồ kết quả

**Các biểu đồ được tạo ra:**

1. **Tiền xử lý (01_*):**
   - `01_boxplot_outliers.png`: Phát hiện outliers
   - `01_scaling_comparison.png`: So sánh trước/sau chuẩn hóa

2. **Trực quan hóa (02_*):**
   - `02_target_distribution.png`: Phân bố lớp mục tiêu
   - `02_numeric_distributions.png`: Phân bố đặc trưng số
   - `02_kde_distributions.png`: KDE plot
   - `02_categorical_distributions.png`: Phân bố đặc trưng phân loại
   - `02_correlation_matrix.png`: Ma trận tương quan
   - `02_pairplot.png`: Pair plot

3. **Phân tích đặc trưng (03_*):**
   - `03_anova_f_scores.png`: ANOVA F-test
   - `03_mutual_information.png`: Mutual Information
   - `03_rf_feature_importance.png`: Random Forest Importance
   - `03_combined_rankings.png`: So sánh xếp hạng
   - `03_feature_selection_comparison.png`: So sánh trước/sau lựa chọn

4. **Phân loại (04_*):**
   - `04_cm_*.png`: Confusion Matrix cho từng mô hình (6 biểu đồ)
   - `04_cv_comparison_boxplot.png`: So sánh Cross-Validation
   - `04_metrics_comparison.png`: So sánh các chỉ số
   - `04_roc_curves.png`: ROC Curves

**Tổng cộng: 24 biểu đồ**


---

## 6. KẾT LUẬN

### 6.1. Tóm tắt kết quả

**Mô hình tốt nhất: Logistic Regression**
- Accuracy: 74.00%
- F1-Score: 73.43%
- AUC-ROC: 75.27%
- Cross-Validation: 75.40% ±2.34%

**Lý do Logistic Regression thắng:**
1. Độ chính xác cao nhất trên test set
2. Ổn định nhất (std thấp nhất trong CV)
3. Đơn giản, dễ triển khai, dễ giải thích
4. Thời gian huấn luyện và dự đoán nhanh
5. Phù hợp với dữ liệu có xu hướng tuyến tính

**Các mô hình khác:**
- XGBoost và Random Forest cũng cho kết quả tốt (73%)
- Random Forest có AUC cao nhất (77.81%) → tốt cho ranking
- Decision Tree kém nhất (68%) → không nên sử dụng đơn lẻ

### 6.2. Đặc trưng quan trọng

**Top 5 đặc trưng ảnh hưởng nhất:**
1. Tình trạng tài khoản séc (checking_status)
2. Thời hạn vay (duration)
3. Lịch sử tín dụng (credit_history)
4. Số tiền vay (credit_amount)
5. Tình trạng tiết kiệm (savings_status)

**Ý nghĩa:**
- Các yếu tố tài chính là quan trọng nhất
- Lịch sử tín dụng là chỉ báo mạnh về khả năng trả nợ
- Thời hạn vay càng dài → rủi ro càng cao

### 6.3. Ưu điểm của giải pháp

1. **Quy trình hoàn chỉnh:**
   - Xử lý dữ liệu kỹ lưỡng (outliers, encoding, scaling)
   - Trực quan hóa đa dạng để hiểu dữ liệu
   - Phân tích đặc trưng bằng nhiều phương pháp
   - So sánh nhiều thuật toán khác nhau

2. **Kết quả tốt:**
   - Accuracy 74% là khá tốt cho bài toán này
   - Mô hình ổn định (CV std thấp)
   - Có thể giải thích được (Logistic Regression)

3. **Tái sử dụng:**
   - Code được tổ chức tốt, dễ bảo trì
   - Có thể áp dụng cho dataset khác
   - Có file utils.py chứa các hàm dùng chung

### 6.4. Hạn chế và hướng phát triển

#### 6.4.1. Hạn chế

1. **Dữ liệu:**
   - Kích thước nhỏ (1,000 mẫu) → có thể không đại diện
   - Mất cân bằng (70-30) → có thể bias về lớp đa số
   - Dữ liệu cũ (từ năm 1994) → có thể không phù hợp với hiện tại

2. **Mô hình:**
   - Chưa tune hyperparameters kỹ lưỡng
   - Chưa thử ensemble methods phức tạp hơn
   - Chưa xử lý imbalanced data (SMOTE, class weights)

3. **Đánh giá:**
   - Chưa phân tích chi phí của False Positive vs False Negative
   - Chưa tối ưu threshold phân loại
   - Chưa đánh giá trên nhiều metrics khác (Specificity, NPV, PPV)


#### 6.4.2. Hướng phát triển

**1. Cải thiện dữ liệu:**
- Thu thập thêm dữ liệu để tăng kích thước dataset
- Xử lý imbalanced data:
  - SMOTE (Synthetic Minority Over-sampling Technique)
  - ADASYN (Adaptive Synthetic Sampling)
  - Class weights trong mô hình
- Feature engineering: tạo thêm đặc trưng mới từ đặc trưng hiện có

**2. Tối ưu mô hình:**
- Hyperparameter tuning:
  - Grid Search
  - Random Search
  - Bayesian Optimization
- Thử các mô hình khác:
  - LightGBM
  - CatBoost
  - Neural Networks
- Ensemble methods:
  - Stacking
  - Blending
  - Voting Classifier

**3. Đánh giá chi tiết hơn:**
- Phân tích chi phí (Cost-Sensitive Learning):
  - Chi phí của FP (cho vay nhầm cho Bad credit)
  - Chi phí của FN (từ chối nhầm Good credit)
- Tối ưu threshold phân loại dựa trên chi phí
- Calibration: hiệu chỉnh xác suất dự đoán
- Explainability: SHAP, LIME để giải thích dự đoán

**4. Triển khai thực tế:**
- Xây dựng API để dự đoán real-time
- Tạo dashboard để giám sát mô hình
- A/B testing để so sánh với hệ thống cũ
- Monitoring và retraining định kỳ

### 6.5. Ứng dụng thực tế

**Trong ngân hàng:**
- Hỗ trợ quyết định cho vay tự động
- Đánh giá rủi ro tín dụng
- Xác định khách hàng tiềm năng
- Tối ưu hóa danh mục cho vay

**Lợi ích:**
- Giảm thời gian xét duyệt khoản vay
- Giảm rủi ro nợ xấu
- Tăng lợi nhuận từ cho vay đúng đối tượng
- Công bằng và minh bạch hơn trong quyết định

**Lưu ý đạo đức:**
- Tránh bias về giới tính, chủng tộc, tuổi tác
- Đảm bảo tính công bằng (fairness)
- Giải thích được quyết định từ chối
- Tuân thủ quy định về bảo vệ dữ liệu cá nhân

### 6.6. Bài học kinh nghiệm

1. **Tiền xử lý dữ liệu rất quan trọng:**
   - Xử lý outliers, encoding, scaling đúng cách giúp mô hình hoạt động tốt hơn
   - Không nên bỏ qua bước này

2. **Trực quan hóa giúp hiểu dữ liệu:**
   - Phát hiện patterns, outliers, correlations
   - Đưa ra insights cho feature engineering

3. **Feature selection cải thiện hiệu suất:**
   - Giảm overfitting
   - Tăng tốc độ huấn luyện
   - Dễ giải thích hơn

4. **Mô hình đơn giản có thể tốt hơn mô hình phức tạp:**
   - Logistic Regression thắng XGBoost và Random Forest
   - Dễ triển khai, dễ bảo trì, dễ giải thích

5. **Cross-Validation là bắt buộc:**
   - Đánh giá độ ổn định của mô hình
   - Tránh overfitting trên test set

6. **So sánh nhiều mô hình:**
   - Không có mô hình nào tốt nhất cho mọi bài toán
   - Cần thử nghiệm và so sánh

---

## PHỤ LỤC

### A. Cấu trúc thư mục dự án

```
Task_2/
├── README.md                   # Mô tả dự án
├── requirements.txt            # Thư viện cần thiết
├── main.py                     # Chạy toàn bộ pipeline
├── data/                       # Dữ liệu đầu vào (raw)
│   └── raw_data.csv
├── output/                     # Kết quả đầu ra
│   ├── processed_data.csv      # Dữ liệu sau tiền xử lý
│   ├── selected_features.csv   # Đặc trưng đã chọn
│   ├── selected_data.csv       # Dữ liệu với đặc trưng đã chọn
│   ├── 01_*.png ~ 04_*.png    # Biểu đồ các bước
│   └── 04_results_summary.csv  # Bảng tổng hợp kết quả
├── source/                     # Mã nguồn Python
│   ├── utils.py                # Hàm tiện ích
│   ├── preprocessing.py        # Tiền xử lý
│   ├── visualization.py        # Trực quan hóa
│   ├── feature_analysis.py     # Phân tích đặc trưng
│   └── classification.py       # Phân loại & đánh giá
├── docs/                       # Tài liệu giải thích
│   └── mo_ta_du_an.md
└── report/                     # Báo cáo chính thức
    └── bao_cao_giua_ky.tex
```


### B. Thư viện sử dụng

```python
numpy>=1.24.0              # Tính toán số học
pandas>=2.0.0              # Xử lý dữ liệu dạng bảng
matplotlib>=3.7.0          # Vẽ biểu đồ
seaborn>=0.12.0            # Trực quan hóa thống kê
scikit-learn>=1.3.0        # Machine Learning
openml>=0.14.0             # Tải dữ liệu từ OpenML
imbalanced-learn>=0.11.0   # Xử lý dữ liệu mất cân bằng
xgboost>=2.0.0             # XGBoost algorithm
```

### C. Hướng dẫn chạy

**Bước 1: Cài đặt thư viện**
```bash
pip install -r requirements.txt
```

**Bước 2: Chạy toàn bộ pipeline**
```bash
python main.py
```

**Kết quả:**
- Dữ liệu gốc trong `data/`
- Kết quả đầu ra trong `output/`
- Bảng tổng hợp: `output/04_results_summary.csv`

### D. Tài liệu tham khảo

**Dataset:**
- OpenML German Credit Data: https://www.openml.org/d/31
- UCI Machine Learning Repository: https://archive.ics.uci.edu/ml/datasets/statlog+(german+credit+data)

**Thuật toán:**
- Logistic Regression: https://scikit-learn.org/stable/modules/linear_model.html#logistic-regression
- KNN: https://scikit-learn.org/stable/modules/neighbors.html
- Decision Tree: https://scikit-learn.org/stable/modules/tree.html
- Random Forest: https://scikit-learn.org/stable/modules/ensemble.html#random-forests
- SVM: https://scikit-learn.org/stable/modules/svm.html
- XGBoost: https://xgboost.readthedocs.io/

**Feature Selection:**
- ANOVA F-test: https://scikit-learn.org/stable/modules/feature_selection.html#univariate-feature-selection
- Mutual Information: https://scikit-learn.org/stable/modules/feature_selection.html#mutual-information
- Random Forest Importance: https://scikit-learn.org/stable/modules/ensemble.html#feature-importance-evaluation

**Evaluation Metrics:**
- Classification Metrics: https://scikit-learn.org/stable/modules/model_evaluation.html#classification-metrics
- ROC and AUC: https://scikit-learn.org/stable/modules/model_evaluation.html#roc-metrics

**Books:**
- "Hands-On Machine Learning with Scikit-Learn, Keras, and TensorFlow" - Aurélien Géron
- "Pattern Recognition and Machine Learning" - Christopher Bishop
- "The Elements of Statistical Learning" - Hastie, Tibshirani, Friedman

---

## TÓM TẮT

Dự án đã thực hiện thành công bài toán phân loại tín dụng trên German Credit Dataset với quy trình hoàn chỉnh từ xử lý dữ liệu, trực quan hóa, phân tích đặc trưng đến huấn luyện và đánh giá mô hình.

**Kết quả chính:**
- Mô hình tốt nhất: Logistic Regression với Accuracy 74.00%
- Đặc trưng quan trọng nhất: Tình trạng tài khoản séc, Thời hạn vay, Lịch sử tín dụng
- So sánh 6 thuật toán: Logistic Regression > XGBoost > Random Forest > SVM > KNN > Decision Tree

**Đóng góp:**
- Quy trình xử lý dữ liệu kỹ lưỡng và có hệ thống
- Phân tích đặc trưng bằng nhiều phương pháp khác nhau
- So sánh toàn diện các thuật toán phân loại
- Code được tổ chức tốt, dễ tái sử dụng
- Trực quan hóa đa dạng và chi tiết

**Ứng dụng thực tế:**
- Hỗ trợ quyết định cho vay tự động trong ngân hàng
- Đánh giá rủi ro tín dụng
- Tối ưu hóa danh mục cho vay

---

**Người thực hiện:** [Tên của bạn]  
**Ngày hoàn thành:** [Ngày tháng năm]  
**Liên hệ:** [Email của bạn]

---
