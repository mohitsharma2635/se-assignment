import React, { useState } from "react";
import ReactSelect from "react-select";
import {
    assignUserToPlanProcedure,
    removeAllUsersFromPlanProcedure,
    removeUserFromPlanProcedure,
} from "../../../api/api";

const PlanProcedureItem = ({ planId, procedure, users, assignedUsers }) => {
    const [selectedUsers, setSelectedUsers] = useState(assignedUsers ?? []);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState(null);

    const saveChange = async (apiCall, newSelectedUsers) => {
        setIsSaving(true);
        setError(null);
        try {
            await apiCall();
            setSelectedUsers(newSelectedUsers);
        } catch (e) {
            setError(e.message);
        } finally {
            setIsSaving(false);
        }
    };

    const handleRemoveAllUsers = () =>
        saveChange(
            () => removeAllUsersFromPlanProcedure(planId, procedure.procedureId),
            []
        );

    const handleUserSelectionChange = (newValue, actionMeta) => {
        switch (actionMeta.action) {
            case "select-option":
                return saveChange(
                    () => assignUserToPlanProcedure(planId, procedure.procedureId, actionMeta.option.value),
                    newValue
                );
            case "remove-value":
            case "pop-value":
                return saveChange(
                    () => removeUserFromPlanProcedure(planId, procedure.procedureId, actionMeta.removedValue.value),
                    newValue
                );
            case "clear":
                return handleRemoveAllUsers();
            default:
                return;
        }
    };

    return (
        <div className="py-2">
            <div className="d-flex justify-content-between align-items-center">
                <span>{procedure.procedureTitle}</span>
                {selectedUsers.length > 0 && (
                    <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        disabled={isSaving}
                        onClick={handleRemoveAllUsers}
                    >
                        Remove all users
                    </button>
                )}
            </div>

            <ReactSelect
                className="mt-2"
                placeholder="Select User to Assign"
                isMulti={true}
                options={users}
                value={selectedUsers}
                isLoading={isSaving}
                isDisabled={isSaving}
                onChange={handleUserSelectionChange}
            />

            {error && <div className="text-danger small mt-1">{error}</div>}
        </div>
    );
};

export default PlanProcedureItem;
