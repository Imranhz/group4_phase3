// Group4Flight - site.js

$(function () {
    // Initialize Bootstrap Date Range Picker on the filter form
    if ($('#daterange').length) {
        $('#daterange').daterangepicker({
            opens: 'left',
            autoUpdateInput: false,
            locale: {
                format: 'YYYY-MM-DD',
                cancelLabel: 'Clear'
            }
        }, function (start, end) {
            $('#daterange').val(start.format('YYYY-MM-DD') + ' - ' + end.format('YYYY-MM-DD'));
            $('#StartDate').val(start.format('YYYY-MM-DD'));
            $('#EndDate').val(end.format('YYYY-MM-DD'));
        });

        $('#daterange').on('cancel.daterangepicker', function () {
            $(this).val('');
            $('#StartDate').val('');
            $('#EndDate').val('');
        });
    }
});
