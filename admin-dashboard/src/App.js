import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import OrdersDashboard from './pages/OrdersDashboard';
import OrderDetails from './pages/OrderDetails';
import FailedOrders from './pages/FailedOrders';

function App() {
  return (
    <Router>
      <div style={{ display: 'flex', minHeight: '100vh' }}>
        
        {/* Sidebar */}
        <div style={{
          width: '220px',
          background: '#1a1a2e',
          color: 'white',
          padding: '20px'
        }}>
          <h3 style={{ color: '#e94560', marginBottom: '30px' }}>
            ⚙️ Admin Panel
          </h3>
          <nav>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              <li style={{ marginBottom: '12px' }}>
                <Link to="/" style={{ color: 'white', textDecoration: 'none' }}>
                  📊 Dashboard
                </Link>
              </li>
              <li style={{ marginBottom: '12px' }}>
                <Link to="/orders" style={{ color: 'white', textDecoration: 'none' }}>
                  📦 All Orders
                </Link>
              </li>
              <li style={{ marginBottom: '12px' }}>
                <Link to="/failed" style={{ color: 'white', textDecoration: 'none' }}>
                  ❌ Failed Orders
                </Link>
              </li>
            </ul>
          </nav>
        </div>

        {/* Main content */}
        <div style={{ flex: 1, padding: '30px', background: '#f5f5f5' }}>
          <Routes>
            <Route path="/" element={<OrdersDashboard />} />
            <Route path="/orders" element={<OrdersDashboard />} />
            <Route path="/orders/:id" element={<OrderDetails />} />
            <Route path="/failed" element={<FailedOrders />} />
          </Routes>
        </div>

      </div>
    </Router>
  );
}

export default App;