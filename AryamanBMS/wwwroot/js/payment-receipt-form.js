(function () {
    function setOptions(select, html) {
        select.innerHTML = html;
    }

    function hideInvoiceDetails(invoiceDetails) {
        if (invoiceDetails) {
            invoiceDetails.classList.add("is-hidden");
        }
    }

    function showInvoiceDetails(invoiceSelect, elements, shouldAutofillAmount) {
        if (invoiceSelect.selectedIndex <= 0) {
            hideInvoiceDetails(elements.invoiceDetails);

            if (shouldAutofillAmount && elements.amountReceived) {
                elements.amountReceived.value = "";
            }

            return;
        }

        var selected = invoiceSelect.options[invoiceSelect.selectedIndex];
        var amount = parseFloat(selected.dataset.amount || "0");
        var balance = parseFloat(selected.dataset.balance || "0");

        if (elements.invoiceAmount) {
            elements.invoiceAmount.textContent = amount.toFixed(2);
        }

        if (elements.invoiceBalance) {
            elements.invoiceBalance.textContent = balance.toFixed(2);
        }

        if (elements.invoicePaid) {
            elements.invoicePaid.textContent = (amount - balance).toFixed(2);
        }

        if (shouldAutofillAmount && elements.amountReceived) {
            elements.amountReceived.value = balance.toFixed(2);
            elements.amountReceived.max = balance.toFixed(2);
        }

        if (elements.invoiceDetails) {
            elements.invoiceDetails.classList.remove("is-hidden");
        }
    }

    async function loadInvoices(clientId, selectedId, invoiceSelect, elements, shouldAutofillAmount) {
        hideInvoiceDetails(elements.invoiceDetails);

        if (shouldAutofillAmount && elements.amountReceived) {
            elements.amountReceived.value = "";
        }

        setOptions(invoiceSelect, "<option value=''>Loading...</option>");

        if (!clientId) {
            setOptions(invoiceSelect, "<option value=''>Select Client First</option>");
            return;
        }

        try {
            var baseUrl = invoiceSelect.dataset.invoiceUrl;
            var url = baseUrl + "?clientId=" + encodeURIComponent(clientId);

            if (selectedId) {
                url += "&selectedInvoiceId=" + encodeURIComponent(selectedId);
            }

            var response = await fetch(url);

            if (!response.ok) {
                throw new Error("Unable to load invoices.");
            }

            var invoices = await response.json();

            setOptions(invoiceSelect, "<option value=''>Select Invoice</option>");

            if (!invoices.length) {
                setOptions(invoiceSelect, "<option value=''>No pending invoice</option>");
                return;
            }

            invoices.forEach(function (invoice) {
                var option = document.createElement("option");

                option.value = invoice.value;
                option.text = invoice.text;
                option.dataset.amount = invoice.amount;
                option.dataset.balance = invoice.balance;

                if (selectedId && invoice.value.toString() === selectedId.toString()) {
                    option.selected = true;
                }

                invoiceSelect.appendChild(option);
            });

            if (selectedId) {
                showInvoiceDetails(invoiceSelect, elements, shouldAutofillAmount);
            }
        }
        catch (error) {
            console.error(error);
            setOptions(invoiceSelect, "<option value=''>Error loading invoices</option>");
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        var clientSelect = document.getElementById("clientSelect");
        var invoiceSelect = document.getElementById("invoiceSelect");

        if (!clientSelect || !invoiceSelect) {
            return;
        }

        var selectedInvoiceId = invoiceSelect.dataset.selectedInvoice || "";
        var shouldAutofillAmount = invoiceSelect.dataset.mode !== "edit";
        var elements = {
            invoiceDetails: document.getElementById("invoiceDetails"),
            invoiceAmount: document.getElementById("invoiceAmount"),
            invoiceBalance: document.getElementById("invoiceBalance"),
            invoicePaid: document.getElementById("invoicePaid"),
            amountReceived: document.getElementById("amountReceived")
        };

        clientSelect.addEventListener("change", function () {
            loadInvoices(this.value, "", invoiceSelect, elements, shouldAutofillAmount);
        });

        invoiceSelect.addEventListener("change", function () {
            showInvoiceDetails(invoiceSelect, elements, shouldAutofillAmount);
        });

        if (clientSelect.value && selectedInvoiceId) {
            loadInvoices(clientSelect.value, selectedInvoiceId, invoiceSelect, elements, shouldAutofillAmount);
        }
    });
})();
