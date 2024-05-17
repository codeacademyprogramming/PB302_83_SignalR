

$(document).ready(function () {

    $(".book-modal").click(function (e) {
        e.preventDefault();
        let url = this.getAttribute("href");

        fetch(url)
            .then(response => response.text())
            .then(data => {
                $("#quickModal .modal-dialog").html(data)
            })

        $("#quickModal").modal('show');
    })

    $(".order-detail-view").click(function (e) {
        e.preventDefault();
        let url = this.getAttribute("href");

        fetch(url)
            .then(response => response.text())
            .then(data => {
                $("#orderDetailModal .modal-dialog").html(data)
            })

        $("#orderDetailModal").modal('show');
    })

    $(".add-to-basket").click(function (e) {
        e.preventDefault();

        let url = this.getAttribute("href");

        fetch(url)
            .then(response => response.text())
            .then(data => {
                $(".cart-widget .cart-block").remove()
                $(".cart-widget").append(data)
            })
    })
})