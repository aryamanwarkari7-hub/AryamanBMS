(function () {
    $(function () {
        var itemTable = $("#itemTable");
        var rowIndex = itemTable.find("tbody tr").length;

        function isProforma() {
            return $("#InvoiceType").val() === "Proforma Invoice";
        }

        function updateClientGstDetails() {
            var selectedOption =
                $("#ClientId").find("option:selected");

            var isExport = isExportUnderLut();
            var gstNo = selectedOption.data("gst") || "";
            var billingAddress =
                selectedOption.data("address") || "";
            var stateCode =
                selectedOption.data("state-code") || "";

            $("#CustomerStateCode").val(
                isExport ? "" : stateCode);

            $("#PlaceOfSupplyStateCode").val(
                isExport ? "" : stateCode);

            $("#GSTNo").val(
                isExport ? "" : gstNo);

            $("#BillingAddress").val(billingAddress);
        }

        function isExportUnderLut() {
            var selectedOption =
                $("#ClientId").find("option:selected");

            return selectedOption.data("tax-treatment") ===
                "ExportUnderLUT";
        }

        function isZeroRated() {
            return isProforma() || isExportUnderLut();
        }

        function filterBillingMilestones() {
            var workOrderId = $("#PurchaseWorkOrderId").val();
            var milestoneDropdown = $("#BillingMilestoneId");
            var currentMilestoneId = milestoneDropdown.val();

            milestoneDropdown.find("option").each(function () {
                var option = $(this);

                if (!option.val()) {
                    option.prop("hidden", false);
                    option.prop("disabled", false);
                    return;
                }

                var milestoneWorkOrderId =
                    option.attr("data-work-order-id");

                var matchesSelectedOrder =
                    workOrderId &&
                    String(milestoneWorkOrderId) === String(workOrderId);

                option.prop("hidden", !matchesSelectedOrder);
                option.prop("disabled", !matchesSelectedOrder);
            });

            if (!workOrderId) {
                milestoneDropdown.val("");
                milestoneDropdown.prop("disabled", true);
                return;
            }

            milestoneDropdown.prop("disabled", false);

            var selectedOption =
                milestoneDropdown.find(
                    'option[value="' + currentMilestoneId + '"]'
                );

            if (
                currentMilestoneId &&
                selectedOption.length &&
                !selectedOption.prop("disabled")
            ) {
                milestoneDropdown.val(currentMilestoneId);
            }
            else {
                milestoneDropdown.val("");
            }
        }

        function updateInvoiceTypeUI() {
            var proforma = isProforma();
            var exportUnderLut = isExportUnderLut();
            var zeroRated = isZeroRated();

            $(".gst-section").toggle(!proforma);
            $(".gst-only-section").toggle(!zeroRated);
            $(".gst-column").toggle(!zeroRated);
            $(".gst-summary-row").toggle(!zeroRated);

            $("#exportLutInvoiceNotice").toggleClass(
                "d-none",
                !exportUnderLut);

            $(".gst").prop("disabled", zeroRated);

            if (zeroRated) {
                $("#IsInterState").prop("checked", false);
                $(".gst").val("0");
                $(".gstAmount").val("0.00");
                $("#GSTAmount").val("0.00");
            }
            else {
                $(".gst").each(function () {
                    var currentValue =
                        parseFloat($(this).val()) || 0;

                    if (currentValue === 0) {
                        $(this).val("18");
                    }
                });
            }

            calculateTotals();
        }

        function buildRow(index) {
            var defaultGst = isZeroRated() ? 0 : 18;

            return `
                <tr>
                    <td>
                        <input name="InvoiceDetails[${index}].ItemName"
                               class="app-input" />
                    </td>
                    <td>
                        <input name="InvoiceDetails[${index}].Description"
                               class="app-input" />
                    </td>
                    <td>
                        <input name="InvoiceDetails[${index}].Qty"
                               class="app-input qty"
                               type="number"
                               step="0.01"
                               value="1" />
                    </td>
                    <td>
                        <select name="InvoiceDetails[${index}].Unit"
                                class="app-select">
                            <option value="Service">Service</option>
                            <option value="Hour">Hour</option>
                            <option value="Day">Day</option>
                            <option value="Month">Month</option>
                            <option value="Project">Project</option>
                            <option value="Job">Job</option>
                            <option value="User">User</option>
                            <option value="License">License</option>
                            <option value="Milestone">Milestone</option>
                        </select>
                    </td>
                    <td>
                        <input name="InvoiceDetails[${index}].Rate"
                               class="app-input rate"
                               type="number"
                               step="0.01"
                               value="0" />
                    </td>
                    <td class="gst-column">
                        <input name="InvoiceDetails[${index}].GSTPercent"
                               class="app-input gst"
                               type="number"
                               step="0.01"
                               value="${defaultGst}" />
                    </td>
                    <td class="gst-column">
                        <input name="InvoiceDetails[${index}].GSTAmount"
                               class="app-input gstAmount"
                               value="0.00"
                               readonly />
                    </td>
                    <td>
                        <input name="InvoiceDetails[${index}].Amount"
                               class="app-input amount"
                               value="0.00"
                               readonly />
                    </td>
                    <td>
                        <button type="button"
                                class="btn-app btn-icon btn-danger-app removeRow">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>`;
        }

        function reIndex() {
            itemTable.find("tbody tr").each(function (index) {
                $(this).find("input, select").each(function () {
                    var name = $(this).attr("name");

                    if (name) {
                        name = name.replace(
                            /InvoiceDetails\[\d+\]/,
                            "InvoiceDetails[" + index + "]");

                        $(this).attr("name", name);
                    }
                });
            });

            rowIndex = itemTable.find("tbody tr").length;
        }

        function calculateTotals() {
            var subTotal = 0;
            var gstTotal = 0;
            var zeroRated = isZeroRated();
            var rows = [];

            itemTable.find("tbody tr").each(function () {
                var row = $(this);
                var qty = parseFloat(row.find(".qty").val()) || 0;
                var rate = parseFloat(row.find(".rate").val()) || 0;
                var gstPercent = zeroRated
                    ? 0
                    : parseFloat(row.find(".gst").val()) || 0;
                var taxableAmount = qty * rate;

                if (zeroRated) {
                    row.find(".gst").val("0");
                }

                row.find(".amount").val(taxableAmount.toFixed(2));

                subTotal += taxableAmount;
                rows.push({
                    row: row,
                    taxableAmount: taxableAmount,
                    gstPercent: gstPercent
                });
            });

            var discount = parseFloat($("#Discount").val()) || 0;

            if (discount < 0) {
                discount = 0;
            }

            if (discount > subTotal) {
                discount = subTotal;
            }

            var allocatedDiscountTotal = 0;

            rows.forEach(function (item, index) {
                var allocatedDiscount = 0;

                if (discount > 0 && subTotal > 0) {
                    var isLastItem = index === rows.length - 1;

                    allocatedDiscount = isLastItem
                        ? discount - allocatedDiscountTotal
                        : Math.round(
                            (
                                discount *
                                item.taxableAmount /
                                subTotal
                            ) * 100) / 100;

                    allocatedDiscount = Math.min(
                        Math.max(allocatedDiscount, 0),
                        item.taxableAmount);

                    allocatedDiscountTotal += allocatedDiscount;
                }

                var taxableAfterDiscount = Math.max(
                    0,
                    item.taxableAmount - allocatedDiscount);

                var gstAmount = zeroRated
                    ? 0
                    : taxableAfterDiscount * item.gstPercent / 100;

                item.row.find(".gstAmount").val(gstAmount.toFixed(2));
                gstTotal += gstAmount;
            });

            var grandTotal =
                subTotal -
                discount +
                (zeroRated ? 0 : gstTotal);

            $("#SubTotal").val(subTotal.toFixed(2));
            $("#GSTAmount").val(
                zeroRated ? "0.00" : gstTotal.toFixed(2));
            $("#GrandTotal").val(grandTotal.toFixed(2));
        }

        function loadPurchaseOrderDetails(orderId) {
            var workOrderSelect = $("#PurchaseWorkOrderId");
            var detailsUrl = workOrderSelect.data("work-order-url");

            if (!orderId || !detailsUrl) {
                return;
            }

            $.get(
                detailsUrl,
                { id: orderId },
                function (data) {
                    $("#ClientId")
                        .val(data.clientId)
                        .trigger("change");

                    $("#ProposalId").val(data.proposalId || "");
                    $("#DueDate").val(
                        data.deliveryDueDate
                            ? data.deliveryDueDate.substring(0, 10)
                            : "");

                    if (data.remarks) {
                        $("#Remarks").val(data.remarks);
                    }
                }
            ).fail(function () {
                alert("Purchase Order details could not be loaded.");
            });
        }

        $("#InvoiceType").on("change", updateInvoiceTypeUI);
        $("#ClientId").on("change", function () {
            updateClientGstDetails();
            updateInvoiceTypeUI();
        });

        $("#PurchaseWorkOrderId").on("change", function () {
            filterBillingMilestones();
            loadPurchaseOrderDetails($(this).val());
        });

        $("#btnAddRow").on("click", function () {
            itemTable.find("tbody").append(buildRow(rowIndex));
            rowIndex++;
            updateInvoiceTypeUI();
        });

        $(document).on("click", ".removeRow", function () {
            if (itemTable.find("tbody tr").length > 1) {
                $(this).closest("tr").remove();
                reIndex();
                calculateTotals();
            }
        });

        $(document).on("keyup change", ".qty,.rate,.gst,#Discount", calculateTotals);

        filterBillingMilestones();
        updateInvoiceTypeUI();
    });
})();
