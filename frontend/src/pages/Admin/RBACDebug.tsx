import React from 'react'
import { Card, Spin, Alert } from 'antd'
import { useRoles, usePermissions } from '@/hooks/usePermission'

/**
 * Debug component to troubleshoot RBAC API issues
 * Navigate to /admin/debug-rbac to view
 */
const RBACDebug: React.FC = () => {
  const rolesQuery = useRoles()
  const permissionsQuery = usePermissions()

  return (
    <div style={{ padding: '24px' }}>
      <h1>RBAC Debug Information</h1>

      {/* Roles Section */}
      <Card title="Roles API" style={{ marginBottom: 24 }}>
        {rolesQuery.isLoading && <Spin />}
        {rolesQuery.error && (
          <Alert
            type="error"
            message="API Error"
            description={JSON.stringify(rolesQuery.error, null, 2)}
          />
        )}
        {rolesQuery.data && (
          <div>
            <p>
              <strong>Status:</strong> Success
            </p>
            <p>
              <strong>Count:</strong> {rolesQuery.data.length}
            </p>
            <pre style={{ background: '#f5f5f5', padding: 16, overflow: 'auto' }}>
              {JSON.stringify(rolesQuery.data, null, 2)}
            </pre>
          </div>
        )}
      </Card>

      {/* Permissions Section */}
      <Card title="Permissions API">
        {permissionsQuery.isLoading && <Spin />}
        {permissionsQuery.error && (
          <Alert
            type="error"
            message="API Error"
            description={JSON.stringify(permissionsQuery.error, null, 2)}
          />
        )}
        {permissionsQuery.data && (
          <div>
            <p>
              <strong>Status:</strong> Success
            </p>
            <p>
              <strong>Count:</strong> {permissionsQuery.data.length}
            </p>
            <pre style={{ background: '#f5f5f5', padding: 16, overflow: 'auto', maxHeight: 400 }}>
              {JSON.stringify(permissionsQuery.data, null, 2)}
            </pre>
          </div>
        )}
      </Card>

      {/* Browser Console Instructions */}
      <Card title="Debugging Tips" style={{ marginTop: 24 }}>
        <ol>
          <li>Open browser Developer Tools (F12)</li>
          <li>Check Console tab for errors</li>
          <li>Check Network tab for API calls:
            <ul>
              <li>GET /api/role</li>
              <li>GET /api/systempermission</li>
            </ul>
          </li>
          <li>Verify JWT token in localStorage: <code>localStorage.getItem('token')</code></li>
        </ol>
      </Card>
    </div>
  )
}

export default RBACDebug
