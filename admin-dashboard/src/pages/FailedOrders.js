import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

const API = process.env.REACT_APP_API_URL || 'http://localhost:5292';

export default function FailedOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    axios.get(`${API}/api/orders`)
      .then(res => {
        const failed = res.data.filter(o =>
          o.status === 'Failed' ||
          o.status === 'PaymentFailed' ||
          o.status === 'InventoryFailed' ||
          o.status === 'ShippingFailed'
        );
        setOrders(failed);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <h2>❌ Failed Orders</h2>
      <p style={{ color: 'gray' }}>
        Showing {orders.length} failed order(s)
      </p>

      {orders.length === 0 ? (
        <div style={{ 
          background: 'white', padding: '40px', 
          borderRadius: '8px', textAlign: 'center' 
        }}>
          <p style={{ color: 'green', fontSize: '18px' }}>
            ✅ No failed orders!
          </p>
        </div>
      ) : (
        <table style={{ 
          width: '100%', borderCollapse: 'collapse',
          background: 'white', borderRadius: '8px' 
        }}>
          <thead>
            <tr style={{ background: '#e74c3c', color: 'white' }}>
              <th style={{ padding: '12px', textAlign: 'left' }}>Order ID</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Customer</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Status</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Reason</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Total</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {orders.map(order => (
              <tr key={order.id} style={{ borderBottom: '1px solid #eee' }}>
                <td style={{ padding: '12px' }}>#{order.shortId}</td>
                <td style={{ padding: '12px' }}>{order.customerName}</td>
                <td style={{ padding: '12px' }}>
                  <span style={{ color: 'red' }}>{order.status}</span>
                </td>
                <td style={{ padding: '12px' }}>
                  {order.failureReason || 'See details'}
                </td>
                <td style={{ padding: '12px' }}>€{order.totalAmount}</td>
                <td style={{ padding: '12px' }}>
                  <button onClick={() => navigate(`/orders/${order.id}`)}>
                    View
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}