import React, { useState } from "react";

interface JournalEntryLine {
    accountId: number;
    accountName: string;
    drcr: "DR" | "CR";
    amount: number;
    memo: string;
}

interface JournalEntryModel {
    journalDate: string;
    voucherType: string;
    referenceNo: string;
    memo: string;
    posted: boolean;
}

const JournalEntry: React.FC = () => {
    const [entry, setEntry] = useState<JournalEntryModel>({
        journalDate: new Date().toISOString().substring(0, 10),
        voucherType: "Journal",
        referenceNo: "",
        memo: "",
        posted: false
    });

    const [lines, setLines] = useState<JournalEntryLine[]>([]);

    const [newLine, setNewLine] = useState<JournalEntryLine>({
        accountId: 0,
        accountName: "",
        drcr: "DR",
        amount: 0,
        memo: ""
    });

    const [validationErrors, setValidationErrors] = useState<string[]>([]);

    const updateLine = (
        index: number,
        field: keyof JournalEntryLine,
        value: any
    ) => {
        setLines(prevLines => {
            const updated = [...prevLines];
            updated[index] = {
                ...updated[index],
                [field]: value
            };
            return updated;
        });
    };

    const removeLine = (index: number) => {
        setLines(prevLines => prevLines.filter((_, i) => i !== index));
    };

    const addLine = () => {
        const amount = Number(newLine.amount) || 0;

        if (!newLine.accountName.trim()) {
            setValidationErrors(["Account is required"]);
            return;
        }

        if (amount <= 0) {
            setValidationErrors(["Amount must be greater than zero"]);
            return;
        }

        setLines(prevLines => [...prevLines, { ...newLine, amount }]);

        setNewLine({
            accountId: 0,
            accountName: "",
            drcr: "DR",
            amount: 0,
            memo: ""
        });

        setValidationErrors([]);
    };

    const totalDebit = lines
        .filter(x => x.drcr === "DR")
        .reduce((sum, x) => sum + (Number(x.amount) || 0), 0);

    const totalCredit = lines
        .filter(x => x.drcr === "CR")
        .reduce((sum, x) => sum + (Number(x.amount) || 0), 0);

    const saveJournal = () => {
        const errors: string[] = [];

        if (lines.length === 0)
            errors.push("At least one line item is required.");

        if (totalDebit !== totalCredit)
            errors.push("Debit and Credit totals must match.");

        if (errors.length > 0) {
            setValidationErrors(errors);
            return;
        }

        console.log("Saving Journal Entry", {
            header: entry,
            lines
        });

        alert("Journal Entry Saved");
    };

    const postJournal = () => {
        if (totalDebit !== totalCredit) {
            alert("Journal is not balanced.");
            return;
        }

        setEntry(prevEntry => ({
            ...prevEntry,
            posted: true
        }));

        alert("Journal Posted");
    };

    return (
        <div className="container mt-3">
            <h3>Journal Entry</h3>

            {validationErrors.length > 0 && (
                <div className="alert alert-danger">
                    <ul>
                        {validationErrors.map((e, i) => (
                            <li key={i}>{e}</li>
                        ))}
                    </ul>
                </div>
            )}

            <div className="card mb-3">
                <div className="card-header">General</div>

                <div className="card-body">
                    <div className="form-group">
                        <label>Date</label>
                        <input
                            type="date"
                            className="form-control"
                            value={entry.journalDate}
                            onChange={e =>
                                setEntry(prevEntry => ({
                                    ...prevEntry,
                                    journalDate: e.target.value
                                }))
                            }
                        />
                    </div>

                    <div className="form-group">
                        <label>Voucher Type</label>
                        <input
                            type="text"
                            className="form-control"
                            value={entry.voucherType}
                            onChange={e =>
                                setEntry(prevEntry => ({
                                    ...prevEntry,
                                    voucherType: e.target.value
                                }))
                            }
                        />
                    </div>

                    <div className="form-group">
                        <label>Reference No</label>
                        <input
                            type="text"
                            className="form-control"
                            value={entry.referenceNo}
                            onChange={e =>
                                setEntry(prevEntry => ({
                                    ...prevEntry,
                                    referenceNo: e.target.value
                                }))
                            }
                        />
                    </div>

                    <div className="form-group">
                        <label>Memo</label>
                        <input
                            type="text"
                            className="form-control"
                            value={entry.memo}
                            onChange={e =>
                                setEntry(prevEntry => ({
                                    ...prevEntry,
                                    memo: e.target.value
                                }))
                            }
                        />
                    </div>

                    <div className="form-group">
                        <label>Posted</label>
                        <input
                            type="checkbox"
                            checked={entry.posted}
                            readOnly
                        />
                    </div>
                </div>
            </div>

            <div className="card mb-3">
                <div className="card-header">Line Items</div>

                <div className="card-body">
                    <table className="table table-bordered">
                        <thead>
                            <tr>
                                <th>Account</th>
                                <th>DR/CR</th>
                                <th>Amount</th>
                                <th>Memo</th>
                                <th></th>
                            </tr>
                        </thead>

                        <tbody>
                            {lines.map((line, index) => (
                                <tr key={index}>
                                    <td>
                                        <input
                                            className="form-control"
                                            value={line.accountName}
                                            onChange={e =>
                                                updateLine(
                                                    index,
                                                    "accountName",
                                                    e.target.value
                                                )
                                            }
                                        />
                                    </td>

                                    <td>
                                        <select
                                            className="form-control"
                                            value={line.drcr}
                                            onChange={e =>
                                                updateLine(
                                                    index,
                                                    "drcr",
                                                    e.target.value
                                                )
                                            }
                                        >
                                            <option value="DR">DR</option>
                                            <option value="CR">CR</option>
                                        </select>
                                    </td>

                                    <td>
                                        <input
                                            type="number"
                                            className="form-control"
                                            value={line.amount}
                                            onChange={e =>
                                                updateLine(
                                                    index,
                                                    "amount",
                                                    Number(e.target.value)
                                                )
                                            }
                                        />
                                    </td>

                                    <td>
                                        <input
                                            className="form-control"
                                            value={line.memo}
                                            onChange={e =>
                                                updateLine(
                                                    index,
                                                    "memo",
                                                    e.target.value
                                                )
                                            }
                                        />
                                    </td>

                                    <td>
                                        <button
                                            type="button"
                                            className="btn btn-danger"
                                            onClick={() =>
                                                removeLine(index)
                                            }
                                        >
                                            Remove
                                        </button>
                                    </td>
                                </tr>
                            ))}

                            <tr>
                                <td>
                                    <input
                                        className="form-control"
                                        value={newLine.accountName}
                                        onChange={e =>
                                            setNewLine(prevNewLine => ({
                                                ...prevNewLine,
                                                accountName: e.target.value
                                            }))
                                        }
                                    />
                                </td>

                                <td>
                                    <select
                                        className="form-control"
                                        value={newLine.drcr}
                                        onChange={e =>
                                            setNewLine(prevNewLine => ({
                                                ...prevNewLine,
                                                drcr: e.target.value as "DR" | "CR"
                                            }))
                                        }
                                    >
                                        <option value="DR">DR</option>
                                        <option value="CR">CR</option>
                                    </select>
                                </td>

                                <td>
                                    <input
                                        type="number"
                                        className="form-control"
                                        value={newLine.amount}
                                        onChange={e =>
                                            setNewLine(prevNewLine => ({
                                                ...prevNewLine,
                                                amount: Number(e.target.value)
                                            }))
                                        }
                                    />
                                </td>

                                <td>
                                    <input
                                        className="form-control"
                                        value={newLine.memo}
                                        onChange={e =>
                                            setNewLine(prevNewLine => ({
                                                ...prevNewLine,
                                                memo: e.target.value
                                            }))
                                        }
                                    />
                                </td>

                                <td>
                                    <button
                                        type="button"
                                        className="btn btn-success"
                                        onClick={addLine}
                                    >
                                        Add
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>

                    <div className="row">
                        <div className="col">
                            <strong>Total Debit:</strong> {totalDebit}
                        </div>

                        <div className="col">
                            <strong>Total Credit:</strong> {totalCredit}
                        </div>
                    </div>
                </div>
            </div>

            <button
                type="button"
                className="btn btn-primary mr-2"
                onClick={saveJournal}
            >
                Save
            </button>

            <button
                type="button"
                className="btn btn-danger"
                onClick={postJournal}
            >
                Post
            </button>
        </div>
    );
};

export default JournalEntry;