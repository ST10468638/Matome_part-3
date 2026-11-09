document.addEventListener("DOMContentLoaded", () => {
    const categoryType = document.getElementById("CategoryType");
    const ageFields = document.querySelectorAll("[data-category-age]");
    const distanceField = document.querySelectorAll("[data-category-distance]");

    const updateCategoryFields = () => {
        const isAge = categoryType?.value === "Age";
        ageFields.forEach(element => {
            element.classList.toggle("d-none", !isAge);
            if (!isAge) {
                const input = element.querySelector("input");
                if (input) input.value = "";
            }
        });
        distanceField.forEach(element => {
            element.classList.toggle("d-none", isAge);
            if (isAge) {
                const input = element.querySelector("input");
                if (input) input.value = "";
            }
        });
    };

    if (categoryType) {
        categoryType.addEventListener("change", updateCategoryFields);
        updateCategoryFields();
    }

    const roleSelect = document.getElementById("Role");
    const inviteWrapper = document.getElementById("organiserInviteWrapper");
    const updateInviteVisibility = () => {
        if (roleSelect && inviteWrapper) {
            inviteWrapper.classList.toggle("d-none", roleSelect.value !== "Organiser");
        }
    };

    if (roleSelect) {
        roleSelect.addEventListener("change", updateInviteVisibility);
        updateInviteVisibility();
    }
});
