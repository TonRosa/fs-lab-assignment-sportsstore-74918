import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

const API = process.env.REACT_APP_API_URL || 'http://localhost:5292';

const statusColors = {
  Submitted: '#gray',
  InventoryPending: '#orange',
  InventoryConfirmed: '#blue',
  InventoryFailed: '#red',
  PaymentPending: '#orange',
  PaymentApproved: '#blue',
  PaymentFailed: '#red',
  ShippingPending: '#orange',
  ShippingCreated: '#blue',
  Completed: 'green',
  Failed: 'red'
};

export default function OrdersDashboard() {
  const [orders, setOrders] = useState([]);
  const [filtered, setFiltered] = useState([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [summary, setSummary] = useState({});
  const navigate = useNavigate();

  useEffect(() => {
    fetchOrders();
  }, []);

  const fetchOrders = async () => {
    try {
      const res = await axios.get(`${API}/api/orders`);
      setOrders(res.data);
      setFiltered(res.data);
      calculateSummary(res.data);
      setLoading(false);
    } catch (err) {
      setError('Failed to connect to Order API. Is it running?');
      setLoading(false);
    }
  };

  const calculateSummary = (data) => {
    setSummary({
      total: data.length,
      completed: data.filter(o => o.status === 'Completed').length,
      failed: data.filter(o => o.status === 'Failed' || 
        o.status === 'PaymentFailed' || 
        o.status === 'InventoryFailed').length,
      pending: data.filter(o => o.status !== 'Completed' && 
        o.status !== 'Failed' &&
        o.status !== 'PaymentFailed' &&
        o.status !== 'InventoryFailed').length,
      revenue: data
        .filter(o => o.status === 'Completed')
        .reduce((sum, o) => sum + o.totalAmount, 0)
    });
  };

  const filterByStatus = (status) => {
    setStatusFilter(status);
    if (!status) {
      setFiltered(orders);
    } else {
      setFiltered(orders.filter(o => o.status === status));
    }
  };

  if (loading) return <p>Loading orders...</p>;
  if (error) return <p style={{ color: 'red' }}>{error}</p>;

  return (
    <div>
      <h2>📊 Orders Dashboard</h2>

      {/* Summary Cards */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '24px' }}>
        {[
          { label: 'Total Orders', value: summary.total, color: '#3498db' },
          { label: 'Completed', value: summary.completed, color: '#2ecc71' },
          { label: 'Failed', value: summary.failed, color: '#e74c3c' },
          { label: 'Pending', value: summary.pending, color: '#f39c12' },
          { label: 'Revenue', value: `€${summary.revenue?.toFixed(2)}`, color: '#9b59b6' }
        ].map(card => (
          <div key={card.label} style={{
            background: card.color,
            color: 'white',
            padding: '16px',
            borderRadius: '8px',
            minWidth: '120px',
            textAlign: 'center'
          }}>
            <div style={{ fontSize: '24px', fontWeight: 'bold' }}>
              {card.value}
            </div>
            <div style={{ fontSize: '12px' }}>{card.label}</div>
          </div>
        ))}
      </div>

      {/* Filter */}
      <div style={{ marginBottom: '16px' }}>
        <label>Filter by status: </label>
        <select 
          value={statusFilter} 
          onChange={e => filterByStatus(e.target.value)}
          style={{ padding: '6px', marginLeft: '8px' }}
        >
          <option value="">All</option>
          <option value="Submitted">Submitted</option>
          <option value="InventoryConfirmed">Inventory Confirmed</option>
          <option value="InventoryFailed">Inventory Failed</option>
          <option value="PaymentApproved">Payment Approved</option>
          <option value="PaymentFailed">Payment Failed</option>
          <option value="ShippingCreated">Shipping Created</option>
          <option value="Completed">Completed</option>
          <option value="Failed">Failed</option>
        </select>
        <button 
          onClick={fetchOrders} 
          style={{ marginLeft: '16px', padding: '6px 12px' }}
        >
          🔄 Refresh
        </button>
      </div>

      {/* Orders Table */}
      {filtered.length === 0 ? (
        <p>No orders found.</p>
      ) : (
        <table style={{ 
          width: '100%', 
          borderCollapse: 'collapse',
          background: 'white',
          borderRadius: '8px',
          overflow: 'hidden'
        }}>
          <thead>
            <tr style={{ background: '#1a1a2e', color: 'white' }}>
              <th style={{ padding: '12px', textAlign: 'left' }}>Order ID</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Customer</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Date</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Status</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Total</th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map(order => (
              <tr key={order.id} style={{ borderBottom: '1px solid #eee' }}>
                <td style={{ padding: '12px' }}>
                  <strong>#{order.shortId}</strong>
                </td>
                <td style={{ padding: '12px' }}>{order.customerName}</td>
                <td style={{ padding: '12px' }}>
                  {new Date(order.createdAt).toLocaleDateString()}
                </td>
                <td style={{ padding: '12px' }}>
                  <span style={{
                    padding: '4px 8px',
                    borderRadius: '4px',
                    background: order.status === 'Completed' ? '#2ecc71' :
                                order.status === 'Failed' ? '#e74c3c' :
                                order.status === 'PaymentFailed' ? '#e74c3c' :
                                order.status === 'PaymentApproved' ? '#3498db' :
                                '#f39c12',
                    color: 'white',
                    fontSize: '12px'
                  }}>
                    {order.status}
                  </span>
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