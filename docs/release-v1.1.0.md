# LiveBarTiktok v1.1.0 — Windows

Bản Windows của https://github.com/phamvu65/LiveBarTiktok.

- DJ B hoạt hình đứng sau quầy, vị trí được nâng cao.
- Quầy DJ có LED đổi màu, chạy đuổi và equalizer theo nhịp.
- Đèn moving head mở rộng vùng quét; bổ sung đèn ở các góc.
- Shader nền được đóng gói riêng để nền không che DJ và tia đèn trong bản Windows.
- Master Rules có hành động chúc mừng sinh nhật, hiện kèm tài khoản người tặng quà.

## Cài đặt

1. Tải `LiveBarTiktok-Windows-v1.1.0.zip` từ Release này.
2. Giải nén toàn bộ ra thư mục mới.
3. Cài Node.js 20 trở lên. Lần đầu cần Internet để cài dependency.
4. Nhấp đúp `run.bat`, đợi Bridge, game và Control Panel mở.
5. Vào Test Lab → Demo để thử. Live thật cần TikFinity Desktop.

Gói không kèm nhạc cá nhân. Chép MP3/WAV/OGG vào `DJ_MUSIC` cạnh `run.bat`;
đặt video/ảnh trong `DJ_VIDEO`. Dừng và mở lại game sau khi thay tài nguyên.
Không cần Unity để chạy gói Windows. “Source code (zip)” tự động của GitHub không chứa game đã build.

## Xác minh

Build Windows thành công bằng Unity 6000.2.10f1. Bộ kiểm thử Node đạt 20/20;
security smoke cho Bridge đạt. Đã kiểm tra nội dung ZIP và mã SHA-256.
Kết nối TikTok live thật và từng loại quà phụ thuộc sự kiện do TikFinity gửi.

Giữ Bridge trên loopback mặc định; lần rà soát dependency còn cảnh báo bảo mật.
Xem `SECURITY-REVIEW.vi.md` trong source để biết phạm vi và giới hạn kiểm tra.
