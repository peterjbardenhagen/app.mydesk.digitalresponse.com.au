# Setup Guide

This document covers the environment and permission setup needed to run and administer `MyDesk`, including Azure AI Foundry access.

## Azure AI Foundry Vector Store Permission Error

### Symptom

In Microsoft Foundry, under **Data + indexes > Vector stores**, you may see an error like:

```text
Unable to fetch vector store details
PermissionDenied: The principal `c20526ae-b9b3-424e-b433-781407b33902` lacks the required data action `Microsoft.CognitiveServices/accounts/AIServices/assets/read` to perform `GET /api/projects/{projectName}/vector_stores`.
```

This is fixable.

### What the error means

The signed-in identity, service principal, or managed identity can reach the Foundry project, but it does **not** have the Azure RBAC data permission needed to read Foundry assets such as vector stores.

The missing permission is:

```text
Microsoft.CognitiveServices/accounts/AIServices/assets/read
```

### Most common fix

Grant the affected principal an Azure AI Foundry role on the correct scope.

In most cases, use one of these roles:

- `Azure AI User` for read/use access
- `Azure AI Developer` for broader day-to-day build access

If the user needs to create, edit, attach, or manage Foundry assets, `Azure AI Developer` is usually the safer choice.

## Fix In Azure Portal

### 1. Confirm the affected principal

From the error message, note the principal object ID:

```text
c20526ae-b9b3-424e-b433-781407b33902
```

Determine whether it is:

- your user account
- a service principal / app registration
- a managed identity

### 2. Open the correct Azure resource

In Azure Portal:

1. Go to the Azure AI Foundry / Azure AI Services resource shown in the error.
2. In this case, the resource shown is:
   - `peter-mmk98bxd-eastus2`
3. Open that resource, not just the project UI.

If your project is connected through a hub/project model, assign the role at the narrowest scope that still works:

1. Project scope, if available
2. Azure AI resource scope
3. Resource group scope, only if necessary

### 3. Add the role assignment

1. In the resource, open **Access control (IAM)**.
2. Click **Add**.
3. Click **Add role assignment**.
4. Search for one of these roles:
   - `Azure AI User`
   - `Azure AI Developer`
5. Select the role.
6. Click **Next**.
7. Under **Members**, choose the affected identity.
8. If you cannot find it by name, search by the object ID or locate it first in Microsoft Entra ID.
9. Review and assign.

### 4. Wait for RBAC propagation

After assigning the role:

1. Wait 2 to 10 minutes.
2. Sign out of Foundry and back in, or refresh the browser.
3. Return to **Data + indexes > Vector stores**.

## How To Find The Principal By Object ID

If you only have the GUID from the error:

1. Go to **Microsoft Entra ID**.
2. Check these locations:
   - **Users**
   - **Enterprise applications**
   - **App registrations**
   - **Managed identities**
3. Search for:

```text
c20526ae-b9b3-424e-b433-781407b33902
```

Once found, use that identity in the role assignment step above.

## CLI Option

If you prefer Azure CLI, assign the role with a command like this:

```bash
az role assignment create \
  --assignee-object-id c20526ae-b9b3-424e-b433-781407b33902 \
  --assignee-principal-type ServicePrincipal \
  --role "Azure AI Developer" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.CognitiveServices/accounts/peter-mmk98bxd-eastus2
```

Notes:

- Change `--assignee-principal-type` if the principal is actually a `User` or `ManagedIdentity`.
- Replace `<subscription-id>` and `<resource-group>` with the real values.
- If you only want read/use access, try `Azure AI User` instead.

## If It Still Fails

If the role assignment is in place and the error remains:

1. Confirm the role was assigned to the **same principal ID** shown in the error.
2. Confirm the role is assigned on the **correct scope**.
3. Confirm you are opening the same Foundry project tied to that resource.
4. Wait a few more minutes for propagation.
5. Check whether a Conditional Access policy or PIM activation is required.
6. If using a managed identity, confirm the application is actually authenticating as that identity.

## Recommended Minimum For This Project

For developers/admins who need to work with Foundry assets in this MyDesk environment:

1. Assign `Azure AI Developer` on the Azure AI resource `peter-mmk98bxd-eastus2`.
2. Refresh access after propagation.
3. Confirm Vector stores load successfully.

## Quick Verification Checklist

After the fix:

1. Open Microsoft Foundry.
2. Open the project.
3. Go to **Data + indexes**.
4. Open **Vector stores**.
5. Confirm the page loads without:
   - `PermissionDenied`
   - `Microsoft.CognitiveServices/accounts/AIServices/assets/read`
6. Confirm existing vector stores are visible.

## Related Notes

- This issue is a **permissions problem**, not an application code bug inside `MyDesk`.
- The fix is normally done in Azure RBAC, not in this repository.
- Microsoft guidance referenced by the portal:
  - `https://aka.ms/FoundryPermissions`
