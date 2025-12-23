$(document).ready(function () {
    const $courseSearch = $("#courseSearch");
    const $courseDropdown = $("#courseDropdown");
    const $clearCourse = $("#clearCourse");

    const $assignmentSearch = $("#assignmentSearch");
    const $assignmentDropdown = $("#assignmentDropdown");
    const $clearAssignment = $("#clearAssignment");

    const $tableBody = $("#gradeTableBody");

    let currentCourseId = $(".filter-course").data("current-course") || "";
    let currentAssignmentId = $(".filter-assignment").data("current-assignment") || "";

    // ======================== COURSE ========================
    if (currentCourseId) {
        const $item = $courseDropdown.find(`[data-id='${currentCourseId}']`);
        if ($item.length) {
            $courseSearch.val($item.text().trim());
            $item.addClass("active");
            $clearCourse.show();
        }
    }

    // 🔍 Tìm kiếm khóa học
    $courseSearch.on("focus input", function () {
        const keyword = $(this).val().toLowerCase().trim();
        $courseDropdown.children("li").each(function () {
            $(this).toggle($(this).text().toLowerCase().includes(keyword));
        });
        $courseDropdown.show();
    });

    // ✅ Khi chọn khóa học
    $courseDropdown.on("click", "li", function () {
        const id = $(this).data("id");
        const name = $(this).text().trim();
        currentCourseId = id;
        $courseSearch.val(name);
        $courseDropdown.hide();
        $clearCourse.show();
        $courseDropdown.find("li").removeClass("active");
        $(this).addClass("active");

        // 🟢 load danh sách bài tập của khóa học đó
        loadAssignments(id);

        // 🟢 load danh sách học viên đã nộp trong khóa học đó
        loadGrades(id, null);
    });


    // ❌ Xóa khóa học
    $clearCourse.on("mousedown", function (e) {
        e.preventDefault();
        e.stopPropagation();
        setTimeout(() => {
            $courseSearch.val("");
            $clearCourse.hide();
            currentCourseId = "";
            $courseDropdown.find("li").removeClass("active");
            $courseDropdown.show();
            $assignmentDropdown.html(""); // reset bài tập
            $assignmentSearch.val("");
            $clearAssignment.hide();
            //$tableBody.html("");
        }, 60);
    });

    // ======================== ASSIGNMENT ========================

    function filterAssignments(keyword) {
    const lower = keyword.toLowerCase();
    let visible = 0;

    $assignmentDropdown.find("li").each(function () {
        const text = $(this).text().toLowerCase();
        const match = text.includes(lower);
        $(this).toggle(match);
        if (match) visible++;
    });

    if (visible === 0) {
        if ($assignmentDropdown.find(".no-result").length === 0) {
            $assignmentDropdown.append(
                '<li class="list-group-item text-muted text-center fst-italic no-result">Không có bài tập</li>'
            );
        }
    } else {
        $assignmentDropdown.find(".no-result").remove();
    }
}

    $assignmentSearch.on("focus input", function () {
    let keyword = $(this).val().trim();

    const hasNumber = /\d/.test(keyword);

    if (keyword.length < 3 || hasNumber) {
        $assignmentDropdown.hide();

        if (hasNumber) {
            $(this).addClass("is-invalid");
            if (!$("#assignmentWarning").length) {
                $(this)
                    .after(
                        '<div id="assignmentWarning" class="invalid-feedback d-block small text-danger mt-1">❌ Tên bài tập không được chứa số.</div>'
                    );
            }
        } else {
            $(this).removeClass("is-invalid");
            $("#assignmentWarning").remove();
        }

        return; 
    }

    $(this).removeClass("is-invalid");
    $("#assignmentWarning").remove();

    filterAssignments(keyword);
    $assignmentDropdown.show();
});

$assignmentDropdown.on("click", "li", function () {
    const id = $(this).data("id");
    if (!id) return;
    const name = $(this).text().trim();

    currentAssignmentId = id;
    $assignmentSearch.val(name);
    $assignmentDropdown.hide();
    $clearAssignment.show();
$assignmentDropdown.find("li").removeClass("active");
$(this).addClass("active");

loadGrades(currentCourseId, id);
});

$clearAssignment.on("mousedown", function (e) {
    e.preventDefault();
    e.stopPropagation();
    setTimeout(() => {
         $assignmentSearch.val("");
           $clearAssignment.hide();
         currentAssignmentId = "";
         $assignmentDropdown.find("li").removeClass("active");
         $assignmentDropdown.show();
         $assignmentSearch.focus();

         if (currentCourseId) loadGrades(currentCourseId, null);
    }, 60);
    });

$(document).on("click", function (e) {
    if (!$(e.target).closest(".filter-course").length) $courseDropdown.hide();
    if (!$(e.target).closest(".filter-assignment").length) $assignmentDropdown.hide();
});


    function loadAssignments(courseId) {
        $.ajax({
            url: `/Instructor/Grade`,
            type: "GET",
            data: { courseId },
            success: function (html) {
                const $page = $(html);
                const newList = $page.find("#assignmentDropdown").html();
                $("#assignmentDropdown").html(newList);
                $("#assignmentSearch").val("");
                $("#clearAssignment").hide();
                currentAssignmentId = "";
            },
            error: () => alert("⚠️ Lỗi khi tải danh sách bài tập!"),
        });
    }

    // 🔹 Load bảng điểm
    function loadGrades(courseId, assignmentId) {
        $tableBody.html(`
            <tr>
                <td colspan="8" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status"></div>
                    <div class="mt-2 text-muted">Đang tải dữ liệu...</div>
                </td>
            </tr>
        `);

        $.ajax({
            url: `/Instructor/Grade`,
            type: "GET",
            data: { courseId, assignmentId },
            success: function (html) {
                const $page = $(html);
                const newTable = $page.find("#gradeTableBody").html() || "";
                $tableBody.hide().html(newTable).fadeIn(200);
            },
            error: () => {
                $tableBody.html(`
                    <tr>
                        <td colspan="8" class="text-center text-danger py-4">
                            ⚠️ Lỗi khi tải dữ liệu điểm.
                        </td>
                    </tr>
                `);
            },
        });
    }

    // ======================== LOAD MẶC ĐỊNH KHI VÀO TRANG ========================
    if (currentCourseId) {
        loadAssignments(currentCourseId); // load dropdown bài tập

        if (currentAssignmentId) {
            // nếu có bài tập hiện tại → chọn sẵn và load điểm
            const $selected = $assignmentDropdown.find(`[data-id='${currentAssignmentId}']`);
            if ($selected.length) {
                $assignmentSearch.val($selected.text().trim());
                $selected.addClass("active");
                $clearAssignment.show();
                loadGrades(currentCourseId, currentAssignmentId);
            }
        } else {
            // nếu chưa có assignment → load bảng điểm tổng
            loadGrades(currentCourseId, null);
        }
    }
});
