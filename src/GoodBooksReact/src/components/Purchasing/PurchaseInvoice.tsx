import * as React from "react";
import { observer } from "mobx-react";
import accounting from "accounting";

import SelectVendor from "../Shared/Components/SelectVendor";
import SelectPaymentTerm from "../Shared/Components/SelectPaymentTerm";
import SelectLineItem from "../Shared/Components/SelectLineItem";
import SelectLineMeasurement from "../Shared/Components/SelectLineMeasurement";

import PurchaseInvoiceStore from "../Shared/Stores/Purchasing/PurchaseInvoiceStore";
import PurchaseInvoiceLine from "../Shared/Stores/Purchasing/PurchaseInvoiceLine";

const urlParams = new URLSearchParams(window.location.search);

const purchId = Number(urlParams.get("purchId") || "0");
const invoiceId = Number(urlParams.get("invoiceId") || "0");

const store = new PurchaseInvoiceStore(purchId, invoiceId);

class ValidationErrors extends React.Component {
    render() {
        if (
            !store.validationErrors ||
            store.validationErrors.length === 0
        ) {
            return null;
        }

        return (
            <div className="alert alert-danger">
                <ul>
                    {store.validationErrors.map(
                        (item: string, index: number) => (
                            <li key={index}>{item}</li>
                        )
                    )}
                </ul>
            </div>
        );
    }
}

const ObservedValidationErrors = observer(ValidationErrors);

class EditButton extends React.Component {
    onClickEditButton = (
        event: React.MouseEvent<HTMLAnchorElement>
    ) => {
        event.preventDefault();

        const container = document.getElementById(
            "divPurchaseInvoiceForm"
        );

        if (container) {
            const nodes = container.getElementsByTagName("*");

            for (let i = 0; i < nodes.length; i++) {
                nodes[i].className = nodes[i].className.replace(
                    " disabledControl",
                    ""
                );
            }
        }

        store.changedEditMode(true);
    };

    render() {
        return (
            <a
                href="#"
                id="linkEdit"
                onClick={this.onClickEditButton}
                className={
                    !store.purchaseInvoice.posted &&
                    !store.editMode
                        ? "btn"
                        : "btn inactiveLink"
                }
            >
                <i className="fa fa-edit"></i>
                Edit
            </a>
        );
    }
}

const ObservedEditButton = observer(EditButton);

class SavePurchaseInvoiceButton extends React.Component {
    saveNewPurchaseInvoice = () => {
        store.savePurchaseInvoice();
    };

    render() {
        return (
            <input
                type="button"
                value="Save"
                onClick={this.saveNewPurchaseInvoice}
                className={
                    !store.purchaseInvoice.posted &&
                    store.editMode
                        ? "btn btn-sm btn-primary btn-flat pull-left"
                        : "btn btn-sm btn-primary btn-flat pull-left inactiveLink"
                }
            />
        );
    }
}

const ObservedSavePurchaseInvoiceButton = observer(
    SavePurchaseInvoiceButton
);

class CancelPurchaseInvoiceButton extends React.Component {
    cancelOnClick = () => {
        const baseUrl =
            location.protocol +
            "//" +
            location.hostname +
            (location.port ? ":" + location.port : "") +
            "/";

        window.location.href =
            baseUrl + "purchasing/purchaseinvoices";
    };

    render() {
        return (
            <button
                type="button"
                className="btn btn-sm btn-default btn-flat pull-left"
                onClick={this.cancelOnClick}
            >
                Close
            </button>
        );
    }
}

class PostButton extends React.Component {
    postOnClick = () => {
        store.postInvoice();
    };

    render() {
        return (
            <input
                type="button"
                value="Post"
                onClick={this.postOnClick}
                className={
                    !store.purchaseInvoice.posted &&
                    !store.editMode &&
                    store.purchaseInvoice.readyForPosting
                        ? "btn btn-sm btn-primary btn-flat btn-danger pull-right"
                        : "btn btn-sm btn-secondary pull-right inactiveLink"
                }
            />
        );
    }
}

const ObservedPostButton = observer(PostButton);

class PurchaseInvoiceHeader extends React.Component {
    onChangeInvoiceDate = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        store.changedInvoiceDate(new Date(e.target.value));
    };

    onChangeReferenceNo = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        store.changedReferenceNo(e.target.value);
    };

    render() {
        return (
            <div className="card">
                <div className="card-header">
                    <a
                        data-toggle="collapse"
                        href="#vendor-info"
                        aria-expanded="true"
                        aria-controls="vendor-info"
                    >
                        <i className="fa fa-align-justify"></i>
                    </a>{" "}
                    Vendor Information
                </div>

                <div
                    className="card-body collapse show row"
                    id="vendor-info"
                >
                    <div className="col-sm-6">
                        <div className="row">
                            <div className="col-sm-2">
                                Sn. no.
                            </div>

                            <div className="col-sm-10">
                                <input
                                    type="text"
                                    className="form-control"
                                    value={
                                        store.purchaseInvoice
                                            .referenceNo || ""
                                    }
                                    onChange={
                                        this.onChangeReferenceNo
                                    }
                                />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-sm-2">
                                Vendor
                            </div>

                            <div className="col-sm-10">
                                <SelectVendor
                                    store={store}
                                    selected={
                                        store.purchaseInvoice
                                            .vendorId
                                    }
                                />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-sm-2">
                                Payment Term
                            </div>

                            <div className="col-sm-10">
                                <SelectPaymentTerm
                                    store={store}
                                    selected={
                                        store.purchaseInvoice
                                            .paymentTermId
                                    }
                                />
                            </div>
                        </div>
                    </div>

                    <div className="col-md-6">
                        <div className="row">
                            <div className="col-sm-2">
                                Date
                            </div>

                            <div className="col-sm-10">
                                <input
                                    type="date"
                                    className="form-control"
                                    onChange={
                                        this.onChangeInvoiceDate
                                    }
                                    value={
                                        store.purchaseInvoice
                                            .invoiceDate
                                            ? store.purchaseInvoice.invoiceDate
                                                  .toISOString()
                                                  .substring(
                                                      0,
                                                      10
                                                  )
                                            : new Date()
                                                  .toISOString()
                                                  .substring(
                                                      0,
                                                      10
                                                  )
                                    }
                                />
                            </div>
                        </div>

                        {/* Main Amount */}

                        <div className="row">
                            <div className="col-sm-2">
                                Main Amount
                            </div>

                            <div className="col-sm-10">
                                <input
                                    type="text"
                                    className="form-control"
                                    value={accounting.formatMoney(
                                        store.GTotal,
                                        {
                                            symbol: "",
                                            format: "%s%v"
                                        }
                                    )}
                                    readOnly
                                />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-sm-2">
                                Reference no.
                            </div>

                            <div className="col-sm-10">
                                <input
                                    type="text"
                                    className="form-control"
                                    value={
                                        store.purchaseInvoice
                                            .referenceNo || ""
                                    }
                                    onChange={
                                        this.onChangeReferenceNo
                                    }
                                />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-sm-2">
                                Status
                            </div>

                            <div className="col-sm-10">
                                <label>
                                    {
                                        store.purchaseInvoiceStatus
                                    }
                                </label>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}

const ObservedPurchaseInvoiceHeader = observer(
    PurchaseInvoiceHeader
);

class PurchaseInvoiceLines extends React.Component {
    addLineItem = () => {
        if (store.validationLine()) {
            const itemId = Number(
                (
                    document.getElementById(
                        "optNewItemId"
                    ) as HTMLInputElement
                ).value
            );

            const measurementId = Number(
                (
                    document.getElementById(
                        "optNewMeasurementId"
                    ) as HTMLInputElement
                ).value
            );

            const quantity = Number(
                (
                    document.getElementById(
                        "txtNewQuantity"
                    ) as HTMLInputElement
                ).value
            );

            const amount = Number(
                (
                    document.getElementById(
                        "txtNewAmount"
                    ) as HTMLInputElement
                ).value
            );

            const discount = Number(
                (
                    document.getElementById(
                        "txtNewDiscount"
                    ) as HTMLInputElement
                ).value
            );

            const code = (
                document.getElementById(
                    "txtNewCode"
                ) as HTMLInputElement
            ).value;

            store.addLineItem(
                0,
                itemId,
                measurementId,
                quantity,
                amount,
                discount,
                code
            );

            (
                document.getElementById(
                    "txtNewCode"
                ) as HTMLInputElement
            ).value = "";

            (
                document.getElementById(
                    "txtNewQuantity"
                ) as HTMLInputElement
            ).value = "1";

            (
                document.getElementById(
                    "txtNewAmount"
                ) as HTMLInputElement
            ).value = "";

            (
                document.getElementById(
                    "txtNewDiscount"
                ) as HTMLInputElement
            ).value = "";
        }
    };

    onClickRemoveLineItem = (i: number) => {
        store.removeLineItem(i);
    };

    onChangeQuantity = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        store.updateLineItem(
            e.target.name,
            "quantity",
            e.target.value
        );
    };

    onChangeAmount = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        store.updateLineItem(
            e.target.name,
            "amount",
            e.target.value
        );
    };

    onChangeDiscount = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        store.updateLineItem(
            e.target.name,
            "discount",
            e.target.value
        );
    };

    onChangeCode = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        store.updateLineItem(
            e.target.name,
            "code",
            e.target.value
        );
    };

    render() {
        return (
            <div className="card">
                <div className="card-header">
                    <a
                        data-toggle="collapse"
                        href="#line-items"
                    >
                        <i className="fa fa-align-justify"></i>
                    </a>{" "}
                    Line Items
                </div>

                <div
                    className="card-body collapse show table-responsive"
                    id="line-items"
                >
                    <table className="table table-hover">
                        <thead>
                            <tr>
                                <td>No</td>
                                <td>Item</td>
                                <td>Code</td>
                                <td>Measurement</td>
                                <td>Quantity</td>
                                <td>Amount</td>
                                <td>Discount</td>
                                <td>Total</td>
                                <td></td>
                            </tr>
                        </thead>

                        <tbody>
                            {store.purchaseInvoice.purchaseInvoiceLines.map(
                                (line, i) => (
                                    <tr key={i}>
                                        <td>{(i + 1) * 10}</td>

                                        <td>
                                            <SelectLineItem
                                                store={store}
                                                row={i}
                                                selected={
                                                    line.itemId
                                                }
                                            />
                                        </td>

                                        <td>
                                            <input
                                                type="text"
                                                className="form-control"
                                                name={i.toString()}
                                                value={
                                                    line.code || ""
                                                }
                                                onChange={
                                                    this
                                                        .onChangeCode
                                                }
                                            />
                                        </td>

                                        <td>
                                            <SelectLineMeasurement
                                                row={i}
                                                store={store}
                                                selected={
                                                    line.measurementId
                                                }
                                            />
                                        </td>

                                        <td>
                                            <input
                                                type="text"
                                                className="form-control"
                                                name={i.toString()}
                                                value={line.quantity.toString()}
                                                onChange={
                                                    this
                                                        .onChangeQuantity
                                                }
                                            />
                                        </td>

                                        <td>
                                            <input
                                                type="text"
                                                className="form-control"
                                                name={i.toString()}
                                                value={line.amount.toString()}
                                                onChange={
                                                    this
                                                        .onChangeAmount
                                                }
                                            />
                                        </td>

                                        <td>
                                            <input
                                                type="text"
                                                className="form-control"
                                                name={i.toString()}
                                                value={line.discount.toString()}
                                                onChange={
                                                    this
                                                        .onChangeDiscount
                                                }
                                            />
                                        </td>

                                        <td>
                                            {store.getLineTotal(
                                                i
                                            )}
                                        </td>

                                        <td>
                                            <button
                                                type="button"
                                                className="btn btn-box-tool"
                                                onClick={() =>
                                                    this.onClickRemoveLineItem(
                                                        i
                                                    )
                                                }
                                            >
                                                <i className="fa fa-fw fa-times"></i>
                                            </button>
                                        </td>
                                    </tr>
                                )
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        );
    }
}

const ObservedPurchaseInvoiceLines = observer(
    PurchaseInvoiceLines
);

class PurchaseInvoiceTotals extends React.Component {
    render() {
        return (
            <div className="card">
                <div className="card-body">
                    <div className="row">
                        <div className="col-md-2">
                            <label>SubTotal:</label>
                        </div>

                        <div className="col-md-2">
                            {accounting.formatMoney(
                                store.RTotal,
                                {
                                    symbol: "",
                                    format: "%s%v"
                                }
                            )}
                        </div>

                        <div className="col-md-2">
                            <label>Tax:</label>
                        </div>

                        <div className="col-md-2">
                            {accounting.formatMoney(
                                store.TTotal,
                                {
                                    symbol: "",
                                    format: "%s%v"
                                }
                            )}
                        </div>

                        <div className="col-md-2">
                            <label>Total:</label>
                        </div>

                        <div className="col-md-2">
                            {accounting.formatMoney(
                                store.GTotal,
                                {
                                    symbol: "",
                                    format: "%s%v"
                                }
                            )}
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}

const ObservedPurchaseInvoiceTotals = observer(
    PurchaseInvoiceTotals
);

class PurchaseInvoice extends React.Component {
    render() {
        return (
            <div>
                <div id="divActionsTop">
                    <ObservedEditButton />
                </div>

                <div id="divPurchaseInvoiceForm">
                    <ObservedValidationErrors />
                    <ObservedPurchaseInvoiceHeader />
                    <ObservedPurchaseInvoiceLines />
                    <ObservedPurchaseInvoiceTotals />
                </div>

                <div id="divActionsBottom">
                    <ObservedSavePurchaseInvoiceButton />
                    <CancelPurchaseInvoiceButton />
                    <ObservedPostButton />
                </div>
            </div>
        );
    }
}

const ObservedPurchaseInvoice = observer(
    PurchaseInvoice
);

export default ObservedPurchaseInvoice;