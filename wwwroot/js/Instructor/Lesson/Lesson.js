$(document).ready(function () {
    // 🔍 Tìm kiếm bài học theo tiêu đề
    $(".search-lesson").on("input", function () {
        const keyword = removeVietnameseTones($(this).val().toLowerCase().trim());

        $(".lesson-card").each(function () {
            const title = removeVietnameseTones($(this).find("h6").text().toLowerCase());
            $(this).toggle(title.includes(keyword));
        });
    });
    // 🔠 Hàm loại bỏ dấu tiếng Việt
    function removeVietnameseTones(str) {
        return str
            .normalize("NFD")                 // tách các dấu ra khỏi ký tự gốc
            .replace(/[\u0300-\u036f]/g, "")  // xóa các dấu thanh
            .replace(/đ/g, "d")               // thay đ → d
            .replace(/Đ/g, "d");              // thay Đ → d
    }
    // 🗑️ Khi bấm nút Xóa bài học
    $(".btn-delete").on("click", function () {
        const lessonId = $(this).data("id");
        const title = $(this).closest(".lesson-card").find("h6").text().trim();

        if (!lessonId) {
            showToast("Không tìm thấy ID bài học để xóa!", true);
            return;
        }

        if (confirm(`Bạn có chắc muốn xóa bài học "${title}" không?`)) {
            $.ajax({
                url: `/Instructor/DeleteLesson?id=${lessonId}`, // đúng với route bạn có
                type: "DELETE",
                success: function (response) {
                    if (response.success) {
                        showToast(`🗑️ Đã xóa "${title}" thành công!`);
                        $(`.btn-delete[data-id='${lessonId}']`).closest(".lesson-card").remove();
                    }
                    else {
                        showToast("❌ Xóa thất bại: " + (response.message || "Lỗi không xác định!"), true);
                    }
                },
                error: function (xhr) {
                    console.error(xhr.responseText);
                    showToast("❌ Có lỗi khi xóa bài học!", true);
                }
            });
        }
    });
    // 🔔 Hàm hiển thị thông báo nhỏ (toast)
    function showToast(message, isError = false) {
        const toast = $("<div></div>")
            .text(message)
            .addClass("custom-toast")
            .css({
                position: "fixed",
                bottom: "20px",
                right: "20px",
                backgroundColor: isError ? "#dc3545" : "#198754",
                color: "white",
                padding: "12px 20px",
                borderRadius: "6px",
                boxShadow: "0 2px 6px rgba(0,0,0,0.3)",
                zIndex: 9999,
                opacity: 0
            })
            .appendTo("body")
            .animate({ opacity: 1 }, 300)
            .delay(2000)
            .fadeOut(500, function () { $(this).remove(); });
    }
});
