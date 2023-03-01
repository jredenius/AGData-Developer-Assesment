$(function () {
    document.addEventListener('submit', (e) => {
        $('.status').text('processing...')
    });
    $(document).scrollTop(localStorage['administrativeTestPageScroll']);
});
$(document).scroll(function () {
    var scrollVal = $(document).scrollTop();
    localStorage['administrativeTestPageScroll'] = scrollVal;
});
function ShowEditForm() {
    $('.form').show(0);
}
function HideEditForm() {
    $('.form').hide(0);
    $('#SelectedCustomer_Id').val('');
    $('#SelectedCustomer_Name').val('');
    $('#SelectedCustomer_Address').val('');
}