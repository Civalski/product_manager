import { Navigate, useLocation } from 'react-router-dom'

export function TurnstileVerifyPage() {
  const location = useLocation()
  return <Navigate to="/login" state={location.state} replace />
}
