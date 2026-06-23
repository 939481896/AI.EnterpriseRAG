import React from 'react'
import { Card, Spin, Alert } from 'antd'
import { useRoles, usePermissions } from '@/hooks/usePermission'
import { uiText } from '@/config/uiText'

/**
 * Debug component to troubleshoot RBAC API issues
 * Navigate to /admin/debug-rbac to view
 */
const RBACDebug: React.FC = () => {
  const rolesQuery = useRoles()
  const permissionsQuery = usePermissions()

  return (
    <div style={{ padding: '24px' }}>
      <h1>{uiText.debug.title}</h1>

      {/* Roles Section */}
      <Card title={uiText.debug.rolesApi} style={{ marginBottom: 24 }}>
        {rolesQuery.isLoading && <Spin />}
        {rolesQuery.error && (
          <Alert
            type="error"
            message={uiText.debug.apiError}
            description={JSON.stringify(rolesQuery.error, null, 2)}
          />
        )}
        {rolesQuery.data && (
          <div>
            <p>
              <strong>{uiText.debug.status}</strong> {uiText.debug.success}
            </p>
            <p>
              <strong>{uiText.debug.count}</strong> {rolesQuery.data.length}
            </p>
            <pre style={{ background: '#f5f5f5', padding: 16, overflow: 'auto' }}>
              {JSON.stringify(rolesQuery.data, null, 2)}
            </pre>
          </div>
        )}
      </Card>

      {/* Permissions Section */}
      <Card title={uiText.debug.permissionsApi}>
        {permissionsQuery.isLoading && <Spin />}
        {permissionsQuery.error && (
          <Alert
            type="error"
            message={uiText.debug.apiError}
            description={JSON.stringify(permissionsQuery.error, null, 2)}
          />
        )}
        {permissionsQuery.data && (
          <div>
            <p>
              <strong>{uiText.debug.status}</strong> {uiText.debug.success}
            </p>
            <p>
              <strong>{uiText.debug.count}</strong> {permissionsQuery.data.length}
            </p>
            <pre style={{ background: '#f5f5f5', padding: 16, overflow: 'auto', maxHeight: 400 }}>
              {JSON.stringify(permissionsQuery.data, null, 2)}
            </pre>
          </div>
        )}
      </Card>

      {/* Browser Console Instructions */}
      <Card title={uiText.debug.tips} style={{ marginTop: 24 }}>
        <ol>
          <li>{uiText.debug.tips1}</li>
          <li>{uiText.debug.tips2}</li>
          <li>{uiText.debug.tips3}
            <ul>
              <li>GET /api/role</li>
              <li>GET /api/systempermission</li>
            </ul>
          </li>
          <li>{uiText.debug.tips4} <code>localStorage.getItem('token')</code></li>
        </ol>
      </Card>
    </div>
  )
}

export default RBACDebug
