$(document).ready(function () {

    document.querySelectorAll(".lesson-header").forEach(header => {
            header.addEventListener("click", () => {
                header.classList.toggle("active");
            });
        });

    /* =====================================================
       🔍 TÌM KIẾM BÀI TẬP
    ===================================================== */
    $(".search-input").on("input", function () {
        const keyword = removeVietnameseTones($(this).val().toLowerCase().trim());

        if (keyword === "") {
            $(".lesson-group").show();
            $(".assignment-card").show();
            return;
        }

        $(".lesson-group").each(function () {
            let matchFound = false;
            const lessonTitle = removeVietnameseTones($(this).find(".lesson-title").text().toLowerCase());

            $(this).find(".assignment-card").each(function () {
                const title = removeVietnameseTones($(this).find(".assignment-title").text().toLowerCase());

                const isMatch =
                    title.includes(keyword) ||
                    lessonTitle.includes(keyword);

                $(this).toggle(isMatch);
                if (isMatch) matchFound = true;
            });

            $(this).toggle(matchFound);

            // ✅ Nếu tìm thấy kết quả trong lesson, tự mở ra
            if (matchFound) {
                $(this).find(".lesson-assignment-list").addClass("show").collapse("show");
                $(this).find(".lesson-header").addClass("active");
            }
        });
    });

    /* =====================================================
    🔠 HÀM LOẠI BỎ DẤU TIẾNG VIỆT
    ===================================================== */
    function removeVietnameseTones(str) {
        if (!str) return "";
        return str
            .normalize("NFD")                     // tách dấu ra khỏi ký tự
            .replace(/[\u0300-\u036f]/g, "")      // xóa các dấu thanh
            .replace(/đ/g, "d").replace(/Đ/g, "D")// thay đ → d
            .replace(/[^a-zA-Z0-9\s]/g, "");      // loại bỏ ký tự đặc biệt
    }

    /* =====================================================
   🧭 SẮP XẾP DANH SÁCH BÀI TẬP (theo từng Lesson Group)
===================================================== */
$(".sort-select").on("change", function () {
    const sortType = $(this).val();

    // Lặp qua từng nhóm lesson để sắp xếp bài tập riêng trong nhóm đó
    $(".lesson-group").each(function () {
        const $lesson = $(this);
        const assignments = $lesson.find(".assignment-card").get();

        assignments.sort((a, b) => {
            const createdA = parseDate($(a).find(".assignment-created").data("created"));
            const createdB = parseDate($(b).find(".assignment-created").data("created"));

            const dueA = parseDate($(a).find(".meta span:nth-child(2)").text());
            const dueB = parseDate($(b).find(".meta span:nth-child(2)").text());
            const scoreA = parseInt($(a).find(".meta strong").text()) || 0;
            const scoreB = parseInt($(b).find(".meta strong").text()) || 0;

            const typeA = normalizeType($(a).find(".meta span:contains('Loại')").text());
            const typeB = normalizeType($(b).find(".meta span:contains('Loại')").text());

            switch (sortType) {
                case "oldest":   return createdA - createdB;
                case "deadline": return dueA - dueB;
                case "type":     return typeOrder(typeA) - typeOrder(typeB);
                case "score":    return scoreB - scoreA;
                default:         return createdB - createdA; // newest
            }
        });

        // Cập nhật lại thứ tự hiển thị trong nhóm
        $lesson.find(".lesson-assignment-list").empty().append(assignments);
    });

    // ✅ Hiển thị thông báo nhỏ
    showToast(`🔄 Đã sắp xếp lại danh sách (${getSortLabel(sortType)})`);
});

/* =====================================================
   🔹 Các hàm phụ trợ
===================================================== */
function normalizeType(text) {
    return text.replace("Loại:", "").trim().toLowerCase();
}

function typeOrder(type) {
    switch (type) {
        case "bài thi": return 1;
        case "bài kiểm tra": return 2;
        case "bài tập": return 3;
        default: return 99;
    }
}

function parseDate(text) {
    // Loại bỏ tiền tố
    const cleaned = text
        .replace("Bắt đầu:", "")
        .replace("Hạn nộp:", "")
        .replace("Tạo lúc:", "")
        .trim();

    // Tách theo ký tự /, :, hoặc khoảng trắng
    const parts = cleaned.split(/[\s/:]/).filter(Boolean);

    // parts = [dd, MM, yyyy, HH, mm, ss]
    if (parts.length >= 6) {
        const [day, month, year, hour, minute, second] = parts.map(p => parseInt(p, 10));
        return new Date(year, month - 1, day, hour || 0, minute || 0, second || 0);
    }
    if (parts.length >= 5) {
        const [day, month, year, hour, minute] = parts.map(p => parseInt(p, 10));
        return new Date(year, month - 1, day, hour || 0, minute || 0);
    }
    return new Date(cleaned) || new Date(0);
}

/* 🔹 Hiển thị nhãn sort */
function getSortLabel(type) {
    switch (type) {
        case "oldest": return "Cũ nhất";
        case "deadline": return "Theo hạn nộp";
        case "type": return "Theo loại bài tập";
        case "score": return "Theo điểm tối đa";
        default: return "Mới nhất";
    }
}
    /* =====================================================
       🗑️ XÓA BÀI TẬP (DÙNG EVENT DELEGATION)
    ===================================================== */
    $(document).on("click", ".btn-delete", function () {
        const id = $(this).data("id");
        const card = $(this).closest(".assignment-card");
        const title = card.find(".assignment-title").text().trim();

        if (!id) return showToast("Không tìm thấy ID bài tập để xóa!", true);
        if (!confirm(`Bạn có chắc muốn xóa bài tập "${title}" không?`)) return;

        $.ajax({
            url: `/Instructor/DeleteAssignment?id=${id}`,
            type: "DELETE",
            success: function (res) {
                if (res.success) {
                    card.fadeOut(300, () => card.remove());
                    showToast(`🗑️ Đã xóa "${title}" thành công!`);
                } else showToast("❌ Xóa thất bại: " + (res.message || "Lỗi không xác định!"), true);
            },
            error: () => showToast("⚠️ Có lỗi khi xóa bài tập!", true)
        });
    });


    /* =====================================================
       🌍 CÔNG KHAI / ẨN BÀI TẬP (DÙNG EVENT DELEGATION)
    ===================================================== */
    $(document).on("click", ".btn-public", function () {
        const id = $(this).data("id");
        const btn = $(this);
        const card = btn.closest(".assignment-card");

        $.ajax({
            url: `/Instructor/TogglePublicAssignment?id=${id}`,
            type: "POST",
            success: function (res) {
                if (res.success) {
                    const badge = card.find(".assignment-status:first span");
                    if (res.isPublic) {
                        badge.removeClass("bg-secondary").addClass("bg-success")
                             .html('<i class="bi bi-globe"></i> Un disable');
                        btn.find("i").removeClass("bi-globe2").addClass("bi-lock");
                        showToast("🌍 Bài tập đã Un disable!");
                    } else {
                        badge.removeClass("bg-success").addClass("bg-secondary")
                             .html('<i class="bi bi-lock"></i> disable');
                        btn.find("i").removeClass("bi-lock").addClass("bi-globe2");
                        showToast("🔒 Bài tập đã disable!");
                    }
                } else showToast(res.message || "Không thể cập nhật trạng thái!", true);
            },
            error: () => showToast("❌ Lỗi khi cập nhật trạng thái disable!", true)
        });
    });


    /* =====================================================
       🔔 THÔNG BÁO NHỎ (TOAST)
    ===================================================== */
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
