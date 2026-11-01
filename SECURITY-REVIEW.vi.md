# Báo cáo rà soát bảo mật và dấu hiệu mã độc

Ngày kiểm tra: 01/11/2026. Phạm vi: workspace `D:\My folder\tiktok-live-bar`.

## Kết luận

Chưa phát hiện bằng chứng mã độc trong mã ứng dụng và script đã kiểm tra. Có các điểm yếu bảo mật cần xử lý, đặc biệt khi mở bridge ra LAN. Đây là rà soát mã nguồn và cấu hình, không phải chứng nhận project sạch virus.

Đã lập danh mục 1.167 file ngoài `.git`, `Library`, `Temp`, `Logs`, `node_modules` và cache audit; quét tĩnh 54 file code/script/giao diện với khoảng 9.021 dòng. Đọc kỹ các đường xử lý mạng, phân quyền, dữ liệu sự kiện, tải tài nguyên, truy cập file và script khởi động/build. Các file hình ảnh, âm thanh và FBX được kiểm kê, không được phân tích toàn bộ cấu trúc nhị phân. Không tuyên bố đã đọc thủ công từng dòng của toàn bộ cache Unity hoặc mọi dependency.

## Phát hiện và ưu tiên

### 1. Cao khi mở LAN: client tự nhận quyền điều khiển

Bằng chứng: `TikTokBridge/server.js:727-755`, `:852-866`; `TikTokBridge/src/security.js`.

Thông điệp `{ "type": "register", "role": "control" }` trực tiếp gán `ws.role`, không yêu cầu token hay mật khẩu. Client có quyền này được lưu Master config, reset game, chạy demo, đổi username và ngắt kết nối live. Kiểm tra Origin không thay thế xác thực: client WebSocket ngoài trình duyệt có thể bỏ hoặc tự đặt Origin.

Đã kiểm chứng logic handler trong môi trường VM với các tác vụ bên ngoài được giả lập: client `nativeClient=false` tự đăng ký `control`, sau đó `reset_game` gọi thành công hàm reset giả lập. Không sửa config thật hay reset phiên live của người dùng.

Điều kiện: mặc định `HOST=127.0.0.1`, `ALLOW_LAN=0` ngăn truy cập trực tiếp từ LAN. Rủi ro tăng rõ khi dùng `HOST=0.0.0.0` cùng `ALLOW_LAN=1`, hoặc proxy/tunnel mở đường truy cập mà không có xác thực. Trên máy cục bộ, tiến trình khác cũng có thể nhận quyền này; đây không phải bằng chứng một website bất kỳ vượt được Origin mặc định.

Khắc phục: xác thực token ngẫu nhiên cho WebSocket trước đăng ký; cấp quyền control từ thông tin xác thực, không tin trường role. Nếu cần LAN, dùng TLS và bảo đảm proxy xác thực cả HTTP lẫn WebSocket, đồng thời chặn truy cập thẳng backend. Tiếp tục giữ cấu hình loopback trong lúc chưa có lớp bảo vệ đó.

### 2. Dependency có cảnh báo bảo mật từ npm registry

Đã chạy `npm.cmd audit --package-lock-only --ignore-scripts --json` thành công sau khi được phép truy cập registry. Kết quả: **5 package bị đánh dấu: 1 critical, 3 high, 1 moderate**. Đây là số package, không phải tổng số advisory.

| Package trong lockfile | Mức npm | Đánh giá khả năng tác động trong project |
|---|---|---|
| `proxy-addr@2.0.7` | Critical | Advisory về giả mạo IP khi dùng cấu hình trust subnet. Project hiện không bật Express `trust proxy`; WebSocket kiểm tra `request.socket.remoteAddress` trực tiếp. Chưa chứng minh khai thác được lỗi này trong cấu hình hiện tại. |
| `axios@1.19.0` | High | Dependency gián tiếp của nhánh TikTok connector/SDK; có advisory DoS, kiểm soát request/proxy và prototype-pollution gadget. Provider mặc định là TikFinity; chưa xác minh các đường lỗi cụ thể được gọi trong nhánh connector. |
| `follow-redirects@1.16.0` | High | Có advisory lộ credential qua redirect trong các kiểu cấu hình header nhất định. Chưa chứng minh cấu hình đó được dùng bởi project. |
| `body-parser@2.3.0` | High | Advisory DoS với compressed request body. Server hiện không gắn middleware parse body, nên chưa thấy endpoint trực tiếp đi qua đường lỗi này. |
| `qs@6.15.3` | Moderate | Các advisory về giới hạn mảng và DoS tùy cách gọi parser/stringify. Chưa chứng minh khai thác được qua endpoint hiện có. |

Bằng chứng phiên bản: `TikTokBridge/package-lock.json:88`, `:100`, `:481`, `:967`, `:989`.

Nguồn advisory trả về từ registry:

- proxy-addr: https://github.com/advisories/GHSA-jqcg-44mw-7w3h
- axios: https://github.com/advisories/GHSA-c29m-xwm3-cm6r và https://github.com/advisories/GHSA-mghh-pgcx-3jjj
- follow-redirects: https://github.com/advisories/GHSA-q3jh-wrc6-jgq8 và https://github.com/advisories/GHSA-mrwp-f3q8-3g9p
- body-parser: https://github.com/advisories/GHSA-27r3-jxmh-p828
- qs: https://github.com/advisories/GHSA-x5fp-wj9c-mxmx và https://github.com/advisories/GHSA-6pq8-jhm2-2vcw

Khắc phục: cập nhật dependency và lockfile trên nhánh riêng, kiểm tra tương thích TikTok connector, chạy lại test/audit. Registry báo có bản sửa cho cả 5 package. Không áp dụng `audit fix` tự động trong lần review này.

### 3. Trung bình: ảnh đại diện có thể làm tăng bộ nhớ không giới hạn

Bằng chứng: `UnityProject/Assets/Scripts/AvatarService.cs:11-13`, `:26-38`, `:54-65`; `TikTokBridge/server.js:411-420`.

AvatarService có giới hạn 4 lượt tải đồng thời và timeout 10 giây, nhưng không giới hạn số URL trong cache/queue, dung lượng tải, kích thước ảnh giải mã hoặc tổng bộ nhớ texture. Sprite/texture đã cache không được thu hồi khi người chơi hết TTL. Giới hạn 400 người chơi không giới hạn tổng số avatar từng tải trong cả phiên. `sessionVipScores` trên Node và bảng donors trong Unity cũng tăng theo tổng số người tặng quà, chỉ xóa khi reset phiên.

Tác động: phiên dài, nhiều URL khác nhau hoặc nguồn ảnh bất thường có thể làm tăng RAM/VRAM và gây treo/crash. Chưa thực hiện thử nghiệm làm cạn bộ nhớ.

Khắc phục: cache LRU có giới hạn, giới hạn queue/byte/pixel, thu hồi texture và sprite khi không còn dùng, giới hạn dữ liệu donor và snapshot.

### 4. Trung bình, tùy quyền kiểm soát nguồn sự kiện: URL ảnh không giới hạn đích

Bằng chứng: `TikTokBridge/src/security.js` hàm `safeHttpsUrl`; `UnityProject/Assets/Scripts/AvatarService.cs:26`, `:54`; `TikTokBridge/public/control.js` hàm `renderGiftCatalog`.

Bridge chỉ kiểm tra giao thức HTTPS, không giới hạn hostname/IP. Kiểm thử xác nhận `https://127.0.0.1:8443/ping` vẫn được chấp nhận làm avatar. Unity có thể gửi request ảnh đến đích đó; bản thân AvatarService còn nhận HTTP nếu có caller khác truyền vào. Đây là bề mặt request tùy ý từ máy chạy game, không phải server Node tự tải ảnh.

Điều kiện: kẻ tấn công phải kiểm soát được URL trong nguồn sự kiện/TikFinity hoặc nguồn cấu hình liên quan. Chưa chứng minh người xem TikTok thông thường tự đặt được URL tùy ý; request HTTPS nội bộ còn phụ thuộc chứng thư hợp lệ. Không thấy đường trả nội dung response về cho kẻ tấn công.

Khắc phục: cho phép các CDN ảnh thực sự cần thiết, chặn địa chỉ loopback/private/link-local, kiểm tra redirect và giới hạn tải. Nếu cần CDN đa dạng, thiết kế đường tải ảnh có chính sách đích và quota rõ ràng.

### 5. Thấp đến trung bình: giới hạn tài nguyên chưa đồng đều giữa các kết nối

Bằng chứng: `TikTokBridge/server.js:46`, `:606`, `:864`; `UnityProject/Assets/Scripts/TikTokWebSocketClient.cs:104-122`.

WebSocket đầu vào control có maxPayload 32 KiB và rate limit theo kết nối, nhưng kết nối nhận từ TikFinity không đặt maxPayload riêng. Unity ghép thông điệp vào MemoryStream và xếp inbox không có giới hạn byte/số lượng; Update xử lý toàn bộ inbox trong một frame. Server cũng không đặt trần số client đồng thời, nên mở nhiều kết nối có thể vượt tổng giới hạn dù mỗi kết nối vẫn đúng rate limit.

Điều kiện: nguồn TikFinity/bridge bị giả mạo hoặc bị chiếm quyền, lưu lượng nguồn quá lớn, hoặc client có thể truy cập bridge. Mặc định loopback giảm bề mặt tấn công từ bên ngoài. Chưa chạy flood test.

Khắc phục: giới hạn maxPayload ở nhánh TikFinity, kích thước thông điệp Unity, độ dài inbox, số sự kiện mỗi frame, số kết nối và tổng lưu lượng.

### 6. Thấp: script tải GIF kiểm tra dung lượng sau khi tải hết

Bằng chứng: `TikTokBridge/scripts/download-mushroom-gifs.js:209-220`.

Giới hạn 1,5 MB chỉ được kiểm tra sau `response.arrayBuffer()`. Nếu nguồn/redirect trả dữ liệu lớn trong khoảng timeout, script vẫn giữ toàn bộ trong RAM trước khi từ chối. URL trong script là danh sách cố định; script không tự chạy khi khởi động server.

Khắc phục: đọc streaming và hủy ngay khi vượt giới hạn; Content-Length chỉ là kiểm tra bổ sung.

## Kiểm tra dấu hiệu mã độc

- Không thấy đường thực thi lệnh tùy ý từ dữ liệu mạng, eval/new Function, tải rồi chạy payload, đánh cắp cookie/mật khẩu, cơ chế khởi động cùng Windows, tác vụ định kỳ hoặc vô hiệu hóa antivirus trong mã ứng dụng/script đã rà soát.
- Hai script `.bat` thực hiện cài dependency, khởi động bridge/game hoặc build bằng Unity. Không thấy hành vi xóa dữ liệu diện rộng. Việc gọi PowerShell trong launcher dùng để kiểm tra health endpoint cục bộ.
- Không có file bắt đầu bằng magic `MZ` trong 1.167 file thuộc phạm vi kiểm kê; chỉ thấy hai script thực thi `run.bat`, `build.bat` theo danh sách phần mở rộng kiểm tra. Không có bản `Build/*.exe` hay ZIP phát hành để đối chiếu.
- `VisualCaptureHarness` chụp cửa sổ game khi có tham số `-capturePath`, lưu ảnh cục bộ rồi thoát; không thấy mã gửi ảnh ra ngoài. Hành vi này phù hợp công cụ kiểm tra giao diện, chưa phải dấu hiệu spyware.
- Không thấy secret dạng private key/token phổ biến qua tìm kiếm mẫu trong phần source/config. `.env` bị gitignore và không hiện diện trong TikTokBridge tại lúc kiểm tra. Đây không phải kiểm toán bí mật toàn bộ lịch sử Git.
- Lockfile có 114 package bên thứ ba, toàn bộ URL resolved trỏ npm registry và có integrity; không có mục hasInstallScript. Điều này hỗ trợ tính tái lập tải gói, không chứng minh gói không chứa mã độc. Chưa tải/đọc source của 114 package.
- Root workspace có `Packages/manifest.json` với Unity AI Assistant và các thư mục Unity sinh ra chưa tracked. Project được build bởi `build.bat` là `UnityProject/`, có manifest riêng. Không coi package Unity chính thức hoặc các cache phát sinh là mã độc chỉ vì có chức năng mạng/chạy công cụ.

## Biện pháp đã có

Mặc định chỉ lắng nghe loopback; từ chối bind LAN nếu chưa bật ALLOW_LAN; kiểm tra Host và Origin; role overlay bị chặn lệnh Master; giới hạn chuỗi/số và giao thức URL sự kiện; maxPayload/rate limit phía control; heartbeat và ngắt client chậm; static file không phục vụ dotfile. Giao diện dùng textContent thay cho ghép HTML với nội dung người xem ở các đường đã đọc.

CSP có `script-src 'unsafe-inline'`: có thể siết sau khi chuyển inline handler/script, nhưng chưa tìm thấy điểm chèn HTML để kết luận có XSS khai thác được.

## Xác minh và giới hạn

- `node --test TikTokBridge/test/*.test.js`: **19/19 đạt**. Test hiện tại không chứng minh client control đã được xác thực.
- Kiểm thử VM handler: client tự nhận control và gọi reset giả lập thành công; kiểm thử helper: LAN Host và Origin trống được chấp nhận trong điều kiện nêu trên.
- npm audit truy vấn registry thành công, kết quả nêu tại mục 2. Mã thoát 1 biểu thị có cảnh báo lỗ hổng.
- Không cài dependency, không chạy server thật/game/Unity, không thực thi script tải GIF hay build, không thay đổi mã ứng dụng/lockfile.
- Chưa chạy security smoke trên server thật vì chưa có node_modules; chưa quét Microsoft Defender/YARA, chưa phân tích động, chưa kiểm chứng binary phát hành hoặc toàn bộ Unity PackageCache. Không thể kết luận các phần này sạch mã độc.

Ưu tiên thực hiện: giữ loopback; bổ sung xác thực trước khi mở LAN; cập nhật dependency; thêm giới hạn ảnh/cache/message. Báo cáo này ghi nhận vấn đề, chưa sửa code.
